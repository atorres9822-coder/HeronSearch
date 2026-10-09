// Student site scripts: password show/hide toggles and the confirm-password match check.
// Replaces the student source form.js; no markup is fetched at runtime (the layout renders it server-side).
(function () {
  // Toggle each password field between hidden and visible when its eye button is clicked.
  document.querySelectorAll('.student-toggle-eye').forEach(function (btn) {
    btn.addEventListener('click', function () {
      var input = btn.parentElement.querySelector('input');
      if (input) {
        input.type = input.type === 'password' ? 'text' : 'password';
      }
    });
  });

  // Keep the confirm-password field in sync so the browser blocks a mismatched submit.
  var password = document.getElementById('Password');
  var confirmPw = document.getElementById('ConfirmPassword');
  if (password && confirmPw) {
    var check = function () {
      confirmPw.setCustomValidity(confirmPw.value === password.value ? '' : 'Passwords do not match');
    };
    password.addEventListener('input', check);
    confirmPw.addEventListener('input', check);
  }
})();
