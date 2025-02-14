document.addEventListener("DOMContentLoaded", function () {
    console.log("DOM loaded"); // Verify script loads

    var form = document.getElementById("registerForm");
    if (form) {
        form.addEventListener("submit", function (e) {
            e.preventDefault();
            console.log("Form submission intercepted");

            // Verify grecaptcha exists
            if (typeof grecaptcha === 'undefined') {
                console.error("reCAPTCHA not loaded");
                return;
            }

            grecaptcha.ready(function () {
                console.log("reCAPTCHA ready");

                grecaptcha.execute('6LeukNYqAAAAAKlG_jBw6OugOCW_jrnpkfSMUzm-', {
                    action: 'register'
                }).then(function (token) {
                    console.log("reCAPTCHA token received:", token);
                    document.getElementById("Captcha").value = token;
                    form.submit(); // Submit AFTER token is set
                }).catch(function (error) {
                    console.error("reCAPTCHA error:", error);
                });
            });
        });
    }
});