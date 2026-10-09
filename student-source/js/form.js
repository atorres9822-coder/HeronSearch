// Show/hide password
document.querySelectorAll('.toggle-eye').forEach(function (btn) {
  btn.addEventListener('click', function () {
    const input = btn.parentElement.querySelector('input');
    input.type = input.type === 'password' ? 'text' : 'password';
  });
});

//check if password and confirm password match
const password = document.getElementById('password');
const confirmPw = document.getElementById('confirm');
if (password && confirmPw) {
  const check = function () {
    confirmPw.setCustomValidity(
      confirmPw.value === password.value ? '' : 'Passwords do not match'
    );
  };
  password.addEventListener('input', check);
  confirmPw.addEventListener('input', check);
}