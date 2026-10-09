/*
 * site.js: shared behavior for every HeronSearch Admin page (vanilla JavaScript, no jQuery).
 * The layouts load it once, after bootstrap.bundle.min.js. It only listens to Bootstrap's modal
 * events and never calls the bootstrap global.
 *
 * Markup contract (views must use these exact names):
 *
 * 1. Password show/hide toggle (Login; Add Staff in Phase 2 has two on one page)
 *
 *      <div class="hs-password">
 *        <input id="Password" type="password" class="form-control" ...>
 *        <button type="button" class="..." data-password-toggle="Password"
 *                aria-controls="Password" aria-label="Show password">
 *          <i class="bi bi-eye" aria-hidden="true"></i>
 *        </button>
 *      </div>
 *
 *    - The input id comes from data-password-toggle; when that value is empty, from aria-controls.
 *    - Masked: icon bi-eye, aria-label "Show password". Shown: icon bi-eye-slash, "Hide password".
 *    - No aria-pressed: the changing accessible name already states the action (Requirement 5.3).
 *    - Submitting the form masks the password again, so browsers never store it as plain text.
 *
 * 2. Confirmation modal (_ConfirmModal partial, rendered once by _Layout)
 *
 *      #confirmModal         the .modal element (aria-labelledby="confirmModalTitle",
 *                            aria-describedby="confirmModalMessage")
 *      #confirmModalTitle    title element; text from data-confirm-title
 *      #confirmModalMessage  message element; text from data-confirm-message
 *      #confirmModalForm     <form method="post"> with the anti-forgery token and
 *                            <input type="hidden" name="returnUrl" value="(current path + query)">
 *      #confirmModalSubmit   <button type="submit" class="btn btn-primary">, plain text only
 *
 *    Trigger: <button type="button" data-bs-toggle="modal" data-bs-target="#confirmModal" ...>
 *      data-confirm-action      required; a local path such as /Items/Delete/12
 *      data-confirm-message     required; names the record ("Delete 'Blue umbrella'? This cannot be undone.")
 *      data-confirm-title       optional; default is the partial's title text
 *      data-confirm-button      optional; default is the partial's button text
 *      data-confirm-variant     optional; primary, secondary, success, danger, or warning (default primary)
 *      data-confirm-return-url  optional local path; default is the partial's returnUrl value
 *
 *    An action that is missing or not a local path ("//host", "/\host", "https://...") leaves the
 *    form without an action and disables the confirm button, so nothing is posted.
 */
(function () {
  'use strict';

  // Accessible names of the password toggle (Requirement 5.3).
  const SHOW_PASSWORD_LABEL = 'Show password';
  const HIDE_PASSWORD_LABEL = 'Hide password';

  // Confirm button styles a trigger may request through data-confirm-variant.
  const CONFIRM_VARIANTS = ['primary', 'secondary', 'success', 'danger', 'warning'];
  const DEFAULT_CONFIRM_VARIANT = 'primary';

  // ASCII control characters; browsers drop tabs and line breaks from URLs, so "/<tab>/host" becomes "//host".
  const CONTROL_CHARACTERS = /[\u0000-\u001F\u007F]/;

  // Runs the callback once the page is parsed, whether the script is deferred or at the end of <body>.
  function onReady(callback) {
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', callback, { once: true });
    } else {
      callback();
    }
  }

  // True for a same-site path: starts with "/" but not "//" or "/\", and has no control characters.
  // Mirrors the server's Url.IsLocalUrl rule for paths.
  function isLocalPath(value) {
    return typeof value === 'string' &&
      value.charAt(0) === '/' &&
      value.charAt(1) !== '/' &&
      value.charAt(1) !== '\\' &&
      !CONTROL_CHARACTERS.test(value);
  }

  // ---- Password show/hide toggles ----

  // Returns the field a toggle controls, or null when its id does not name a password or text input.
  function findPasswordInput(toggle) {
    const id = toggle.getAttribute('data-password-toggle') || toggle.getAttribute('aria-controls');
    const input = id ? document.getElementById(id) : null;
    if (!(input instanceof HTMLInputElement)) {
      return null;
    }
    return input.type === 'password' || input.type === 'text' ? input : null;
  }

  // Updates the toggle's icon and accessible name to match the field's current type.
  function syncPasswordToggle(toggle, input) {
    const isShown = input.type === 'text';
    toggle.setAttribute('aria-label', isShown ? HIDE_PASSWORD_LABEL : SHOW_PASSWORD_LABEL);
    const icon = toggle.querySelector('.bi');
    if (icon) {
      icon.classList.toggle('bi-eye', !isShown);
      icon.classList.toggle('bi-eye-slash', isShown);
    }
  }

  // Click handler for every toggle on the page: switches its field between masked and plain text.
  function handlePasswordToggleClick(event) {
    const toggle = event.target instanceof Element ? event.target.closest('[data-password-toggle]') : null;
    if (!toggle) {
      return;
    }
    event.preventDefault(); // a toggle never submits its form, even without type="button"
    const input = findPasswordInput(toggle);
    if (!input) {
      return;
    }
    input.type = input.type === 'password' ? 'text' : 'password';
    syncPasswordToggle(toggle, input);
  }

  // Submit handler: masks every shown password of the submitted form before the browser sends it.
  function handlePasswordFormSubmit(event) {
    document.querySelectorAll('[data-password-toggle]').forEach(function (toggle) {
      const input = findPasswordInput(toggle);
      if (input && input.form === event.target && input.type === 'text') {
        input.type = 'password';
        syncPasswordToggle(toggle, input);
      }
    });
  }

  // Syncs each toggle with its field, then serves all toggles through two document listeners.
  function initPasswordToggles() {
    document.querySelectorAll('[data-password-toggle]').forEach(function (toggle) {
      const input = findPasswordInput(toggle);
      if (input) {
        syncPasswordToggle(toggle, input);
      }
    });
    document.addEventListener('click', handlePasswordToggleClick);
    document.addEventListener('submit', handlePasswordFormSubmit);
  }

  // ---- Confirmation modal ----

  // Gives the confirm button exactly one allowed btn-* color class (unknown values use primary).
  function setConfirmVariant(button, variant) {
    const chosen = CONFIRM_VARIANTS.indexOf(variant) !== -1 ? variant : DEFAULT_CONFIRM_VARIANT;
    CONFIRM_VARIANTS.forEach(function (name) {
      button.classList.toggle('btn-' + name, name === chosen);
    });
  }

  // Wires #confirmModal: each opening copies the trigger's data-confirm-* values into the modal.
  function initConfirmModal() {
    const modal = document.getElementById('confirmModal');
    const form = document.getElementById('confirmModalForm');
    const submitButton = document.getElementById('confirmModalSubmit');
    if (!modal || !form || !submitButton) {
      return; // Minimal_Layout pages (Login, Signed Out) have no confirm modal
    }
    const title = document.getElementById('confirmModalTitle');
    const message = document.getElementById('confirmModalMessage');
    const returnUrlInput = form.querySelector('input[name="returnUrl"]');

    // Values rendered by the partial, used whenever a trigger leaves one out.
    const defaults = {
      title: title ? title.textContent.trim() : '',
      message: message ? message.textContent.trim() : '',
      button: submitButton.textContent.trim(),
      returnUrl: returnUrlInput ? returnUrlInput.value : ''
    };
    let isSubmitting = false;

    // Fills the modal from a trigger's data-confirm-* values (text only, never HTML).
    // Without a trigger it shows the defaults and disarms the form.
    function configure(trigger) {
      const data = trigger instanceof HTMLElement ? trigger.dataset : {};
      if (title) {
        title.textContent = data.confirmTitle || defaults.title;
      }
      if (message) {
        message.textContent = data.confirmMessage || defaults.message;
      }
      submitButton.textContent = data.confirmButton || defaults.button;
      setConfirmVariant(submitButton, data.confirmVariant);
      if (returnUrlInput) {
        returnUrlInput.value = isLocalPath(data.confirmReturnUrl) ? data.confirmReturnUrl : defaults.returnUrl;
      }
      if (isLocalPath(data.confirmAction)) {
        form.setAttribute('action', data.confirmAction);
        submitButton.disabled = false;
      } else {
        form.removeAttribute('action');
        submitButton.disabled = true;
      }
      isSubmitting = false;
    }

    // Opening: Bootstrap passes the clicked trigger as event.relatedTarget.
    modal.addEventListener('show.bs.modal', function (event) {
      configure(event.relatedTarget);
    });

    // Closed without confirming (Cancel, Escape, backdrop): disarm the form, the record stays unchanged.
    modal.addEventListener('hidden.bs.modal', function () {
      configure(null);
    });

    // Confirming: post only to a valid local action, and only once per opening.
    form.addEventListener('submit', function (event) {
      if (isSubmitting || !isLocalPath(form.getAttribute('action'))) {
        event.preventDefault();
        return;
      }
      isSubmitting = true;
      submitButton.disabled = true;
    });

    configure(null); // disarmed until a trigger opens the modal
  }

  // ---- Claim review: approve checklist ----

  // Enables the Approve button only while every verification checkbox (.js-check) on the page is checked.
  function initApproveChecklist() {
    var checks = Array.prototype.slice.call(document.querySelectorAll('.js-check'));
    var button = document.querySelector('.js-approve-btn');
    if (checks.length === 0 || !button) {
      return;
    }

    // Reflects the current checkbox state onto the button's disabled flag.
    function sync() {
      button.disabled = !checks.every(function (box) { return box.checked; });
    }

    checks.forEach(function (box) {
      box.addEventListener('change', sync);
    });
    sync();
  }

  // ---- Reject modal: reopen after an invalid submission ----

  // Opens #rejectModal on load when it carries data-open-on-load (the server redisplayed Review with an error).
  function initRejectModalReopen() {
    var modal = document.getElementById('rejectModal');
    if (!modal || !modal.getAttribute('data-open-on-load')) {
      return;
    }
    if (window.bootstrap && window.bootstrap.Modal) {
      window.bootstrap.Modal.getOrCreateInstance(modal).show();
    }
  }

  // ---- Change role modal: preselect the current role ----

  // When #changeRoleModal opens, points its form at the user's row action and preselects their current role.
  function initChangeRoleModal() {
    var modal = document.getElementById('changeRoleModal');
    var form = document.getElementById('changeRoleForm');
    var select = document.getElementById('changeRoleSelect');
    if (!modal || !form || !select) {
      return;
    }

    modal.addEventListener('show.bs.modal', function (event) {
      var trigger = event.relatedTarget;
      if (!(trigger instanceof HTMLElement)) {
        return;
      }
      var action = trigger.getAttribute('data-action');
      if (action && isLocalPath(action)) {
        form.setAttribute('action', action);
      }
      var currentRole = trigger.getAttribute('data-current-role');
      if (currentRole) {
        select.value = currentRole;
      }
      // Point the form's hidden Id at the user whose row opened the modal.
      var userIdField = document.getElementById('changeRoleUserId');
      var userId = trigger.getAttribute('data-user-id');
      if (userIdField && userId) {
        userIdField.value = userId;
      }
    });
  }

  // Starts every behavior once the page is ready.
  onReady(function () {
    initPasswordToggles();
    initConfirmModal();
    initApproveChecklist();
    initRejectModalReopen();
    initChangeRoleModal();
  });
})();
