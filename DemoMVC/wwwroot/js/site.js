$(function () {
    const modalElement = document.getElementById("crud-modal");
    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    const modalContent = document.getElementById("crud-modal-content");
    let searchTimer;
    let pendingDeleteForm;

    function showMessage(message, type) {
        $(".ajax-message").remove();
        $("<div>", {
            class: `alert alert-${type} alert-dismissible fade show ajax-message`,
            role: "alert"
        }).text(message)
            .append($("<button>", {
                type: "button",
                class: "btn-close",
                "data-bs-dismiss": "alert",
                "aria-label": "Đóng"
            }))
            .prependTo(".app-container main");
    }

    function loadGrid(url, updateHistory) {
        const results = document.querySelector("[data-grid-results]");
        if (!results) {
            return;
        }

        $.ajax({
            url,
            headers: { "X-Requested-With": "XMLHttpRequest" }
        }).done(function (html) {
            results.innerHTML = html;
            if (updateHistory) {
                window.history.pushState({}, "", url);
            }
        }).fail(function () {
            showMessage("Không thể tải danh sách. Vui lòng thử lại.", "danger");
        });
    }

    function searchUrl(form) {
        const url = new URL(form.action, window.location.origin);
        const search = new FormData(form).get("searchString");
        if (search && search.toString().trim()) {
            url.searchParams.set("searchString", search.toString().trim());
        } else {
            url.searchParams.delete("searchString");
        }
        url.searchParams.set("page", "1");
        return url.toString();
    }

    function reparseValidation(form) {
        if ($.validator && $.validator.unobtrusive) {
            $(form).removeData("validator").removeData("unobtrusiveValidation");
            $.validator.unobtrusive.parse(form);
        }
    }

    $(document).on("click", "[data-modal-url]", function (event) {
        event.preventDefault();
        const trigger = this;
        modalContent.innerHTML = '<div class="modal-content"><div class="modal-body text-center py-5">Đang tải...</div></div>';
        modal.show();

        $.ajax({
            url: trigger.dataset.modalUrl,
            headers: { "X-Requested-With": "XMLHttpRequest" }
        }).done(function (html) {
            modalContent.innerHTML = html;
            const title = modalContent.querySelector(".modal-title");
            if (title && trigger.dataset.modalTitle) {
                title.textContent = trigger.dataset.modalTitle;
            }
            reparseValidation(modalContent);
        }).fail(function () {
            modal.hide();
            showMessage("Không thể tải thông tin. Vui lòng thử lại.", "danger");
        });
    });

    $(document).on("submit", "form[data-ajax-form]", function (event) {
        event.preventDefault();
        const form = this;
        if ($.validator && $(form).data("validator") && !$(form).valid()) {
            return;
        }

        const submitButton = form.querySelector('button[type="submit"]');
        if (submitButton) {
            submitButton.disabled = true;
        }

        $.ajax({
            url: form.action,
            method: form.method || "POST",
            data: $(form).serialize(),
            headers: { "X-Requested-With": "XMLHttpRequest" }
        }).done(function (response) {
            modal.hide();
            showMessage(response.message || "Đã lưu dữ liệu.", "success");
            loadGrid(window.location.href, false);
        }).fail(function (xhr) {
            if (xhr.status === 400 && xhr.responseText) {
                modalContent.innerHTML = xhr.responseText;
                reparseValidation(modalContent.querySelector("form"));
                return;
            }

            const message = xhr.responseJSON && xhr.responseJSON.message
                ? xhr.responseJSON.message
                : "Không thể lưu dữ liệu. Vui lòng thử lại.";
            showMessage(message, "danger");
            if (submitButton) {
                submitButton.disabled = false;
            }
        });
    });

    $(document).on("submit", "form[data-ajax-delete]", function (event) {
        event.preventDefault();
        pendingDeleteForm = this;
        modalContent.innerHTML = `
            <div class="modal-content">
                <div class="modal-header">
                    <h2 class="modal-title fs-5">Xác nhận xóa</h2>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Đóng"></button>
                </div>
                <div class="modal-body"><p class="mb-0" data-delete-message></p></div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-back" data-bs-dismiss="modal">Hủy</button>
                    <button type="button" class="btn btn-danger" data-confirm-delete>Xóa</button>
                </div>
            </div>`;
        modalContent.querySelector("[data-delete-message]").textContent =
            `Bạn có chắc muốn xóa "${this.dataset.deleteLabel}"?`;
        modal.show();
    });

    $(document).on("click", "[data-confirm-delete]", function () {
        const form = pendingDeleteForm;
        if (!form) {
            return;
        }

        const button = this;
        button.disabled = true;

        $.ajax({
            url: form.action,
            method: form.method || "POST",
            data: $(form).serialize(),
            headers: { "X-Requested-With": "XMLHttpRequest" }
        }).done(function (response) {
            pendingDeleteForm = null;
            modal.hide();
            showMessage(response.message || "Đã xóa dữ liệu.", "success");
            loadGrid(window.location.href, false);
        }).fail(function (xhr) {
            pendingDeleteForm = null;
            modal.hide();
            const message = xhr.responseJSON && xhr.responseJSON.message
                ? xhr.responseJSON.message
                : "Không thể xóa dữ liệu. Vui lòng thử lại.";
            showMessage(message, "danger");
        });
    });

    $(document).on("submit", "form[data-grid-search]", function (event) {
        event.preventDefault();
        loadGrid(searchUrl(this), true);
    });

    $(document).on("click", "[data-grid-clear]", function (event) {
        event.preventDefault();
        const form = this.closest("form");
        form.querySelector('[name="searchString"]').value = "";
        loadGrid(searchUrl(form), true);
    });

    $(document).on("click", "[data-grid-results] .pagination a", function (event) {
        const item = this.closest(".page-item");
        if (item && item.classList.contains("disabled")) {
            event.preventDefault();
            return;
        }

        event.preventDefault();
        loadGrid(this.href, true);
    });

    $(document).on("input", "form[data-grid-search] [name='searchString']", function () {
        const form = this.form;
        window.clearTimeout(searchTimer);
        searchTimer = window.setTimeout(function () {
            loadGrid(searchUrl(form), true);
        }, 350);
    });

    window.addEventListener("popstate", function () {
        const searchInput = document.querySelector("form[data-grid-search] [name='searchString']");
        if (searchInput) {
            searchInput.value = new URLSearchParams(window.location.search).get("searchString") || "";
        }
        loadGrid(window.location.href, false);
    });
});
