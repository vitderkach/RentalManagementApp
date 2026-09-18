// Registers client validation for forms inserted after the initial page load.
window.initializeUnobtrusiveValidation = function (container) {
    if (!window.jQuery || !jQuery.validator || !jQuery.validator.unobtrusive) return;

    var forms = jQuery(container).find('form').addBack('form');
    forms.each(function () {
        var form = jQuery(this);
        form.removeData('validator');
        form.removeData('unobtrusiveValidation');
        jQuery.validator.unobtrusive.parse(form);
    });
};

// Generic handling for modal forms that are populated from partial views returned by the
// server. On success, the modal is closed and the page reloads to refresh the affected area.
// On validation failure (HTTP 400), the server re-renders the same partial with validation
// messages, and we swap it back into the modal body in place.
document.addEventListener('submit', function (e) {
    var form = e.target.closest('.ajax-modal-form, .ajax-post-form');
    if (!form) return;

    e.preventDefault();
    var modalBody = form.closest('.modal-content');

    fetch(form.action, {
        method: 'POST',
        body: new FormData(form)
    }).then(function (response) {
        if (response.ok) {
            var modalEl = form.closest('.modal');
            if (modalEl) {
                var instance = bootstrap.Modal.getInstance(modalEl);
                if (instance) instance.hide();
            }
            window.location.reload();
            return;
        }

        return response.text().then(function (html) {
            if (modalBody) {
                modalBody.innerHTML = html;
                window.initializeUnobtrusiveValidation(modalBody);
            } else {
                alert('Action failed: ' + html);
            }
        });
    }).catch(function () {
            if (modalBody) {
                var error = document.createElement('div');
                error.className = 'alert alert-danger m-3';
                error.setAttribute('role', 'alert');
                error.textContent = 'The request could not be completed. Please check your connection and try again.';
                modalBody.prepend(error);
            } else {
                alert('The request could not be completed. Please check your connection and try again.');
            }
        });
    });
