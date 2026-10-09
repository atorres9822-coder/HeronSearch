/*
 * validation-rules.js: two custom client rules for the item form, added on top of
 * jQuery Validation and jQuery Validation Unobtrusive (both loaded first by
 * _ValidationScriptsPartial.cshtml, from wwwroot/lib). No CDN.
 *
 * The server attributes emit the data-* attributes these adapters read:
 *
 *   NotInFutureAttribute (ViewModels/NotInFutureAttribute.cs), rule "notfuture":
 *     data-val            = "true"
 *     data-val-notfuture  = "Date cannot be in the future."
 *
 *   PhotoFileAttribute (ViewModels/PhotoFileAttribute.cs), rule "photo":
 *     data-val                   = "true"
 *     data-val-photo             = "Photo must be a JPG, PNG, or WEBP image."  (type/extension message)
 *     data-val-photo-maxbytes    = 5242880
 *     data-val-photo-extensions  = ".jpg,.jpeg,.png,.webp"
 *     data-val-photo-empty       = "The selected photo is empty."
 *     data-val-photo-toolarge    = "Photo must be 5 MB or smaller."
 *
 * The checks mirror Helpers/ImageUploadValidator.Check: empty, then size, then extension.
 */
(function ($) {
  'use strict';

  // Nothing to wire up when the validation libraries are missing (a non-form page).
  if (!$ || !$.validator || !$.validator.unobtrusive) {
    return;
  }

  // Today in the browser's local time as "yyyy-MM-dd", so it compares as a string with a date input's value.
  function localTodayIso() {
    var now = new Date();
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var day = String(now.getDate()).padStart(2, '0');
    return now.getFullYear() + '-' + month + '-' + day;
  }

  // notfuture method: an empty value passes (Required handles emptiness); otherwise valid when the
  // yyyy-MM-dd value is today or earlier. String comparison works because both sides are zero-padded ISO dates.
  $.validator.addMethod('notfuture', function (value, element) {
    if (!value) {
      return true;
    }
    return value <= localTodayIso();
  });

  // notfuture adapter: a boolean rule with no params; addBool wires the message from data-val-notfuture.
  $.validator.unobtrusive.adapters.addBool('notfuture');

  // photo method: an optional file field. No file passes; otherwise check empty, then size, then
  // extension in that order and set the matching message before failing.
  $.validator.addMethod('photo', function (value, element, params) {
    var files = element.files;
    if (!files || files.length === 0) {
      return true;
    }

    var file = files[0];

    // Empty file (0 bytes).
    if (file.size === 0) {
      setPhotoMessage(this, element, params.empty);
      return false;
    }

    // Larger than the allowed maximum.
    if (file.size > Number(params.maxbytes)) {
      setPhotoMessage(this, element, params.toolarge);
      return false;
    }

    // Extension not in the allowed list (compared lowercased, with the leading dot).
    var allowed = String(params.extensions || '')
      .split(',')
      .map(function (ext) { return ext.trim().toLowerCase(); })
      .filter(function (ext) { return ext.length > 0; });
    var name = String(file.name || '').toLowerCase();
    var dot = name.lastIndexOf('.');
    var extension = dot >= 0 ? name.substring(dot) : '';
    if (allowed.indexOf(extension) === -1) {
      // The default data-val-photo message is the type/extension message.
      setPhotoMessage(this, element, params.message);
      return false;
    }

    return true;
  });

  // Points the active validator's message for this field+rule at the message chosen for the failing case.
  function setPhotoMessage(validator, element, message) {
    if (!validator || !validator.settings || !message) {
      return;
    }
    var messages = validator.settings.messages[element.name] || {};
    messages.photo = message;
    validator.settings.messages[element.name] = messages;
  }

  // photo adapter: reads the four extra params plus the primary data-val-photo message, and hands
  // them to the method so it can pick the right message per failing case.
  $.validator.unobtrusive.adapters.add('photo', ['maxbytes', 'extensions', 'empty', 'toolarge'], function (options) {
    options.rules['photo'] = {
      maxbytes: options.params.maxbytes,
      extensions: options.params.extensions,
      empty: options.params.empty,
      toolarge: options.params.toolarge,
      message: options.message
    };
    options.messages['photo'] = options.message;
  });
})(jQuery);
