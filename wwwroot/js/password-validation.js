document.addEventListener("DOMContentLoaded", function () {
    var passwordField = document.getElementById("Password");
    if (passwordField) {
        passwordField.addEventListener("input", function () {
            const password = this.value;
            const feedback = document.getElementById("passwordFeedback");
            const regex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{12,}$/;
            if (regex.test(password)) {
                feedback.textContent = "Password is strong.";
                feedback.style.color = "green";
            } else {
                feedback.textContent = "Password must be at least 12 characters long and include uppercase, lowercase, number, and special character.";
                feedback.style.color = "red";
            }
        });
    }
});