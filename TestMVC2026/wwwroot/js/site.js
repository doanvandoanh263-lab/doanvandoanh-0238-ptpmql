$(function () {
    const modalElement = document.getElementById("crud-modal");
    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    const modalContent = document.getElementById("crud-modal-content");
    let searchTimer;
    let pendingDeleteForm;

    function showMessage(message, type) {
        $(".ajax-message").remove();
        $("<div>", { class: `alert alert-${type} alert-dismissible fade show ajax-message`, role: "alert" })
            .text(message)
            .append($("<button>", { type: "button", class: "btn-close", "data-bs-dismiss": "alert", "aria-label": "Đóng" }))
            .prependTo("main");
    }

    function loadGrid(url, updateHistory) {
        const results = document.querySelector("[data-grid-results]");
        if (!results) return;
        $.ajax({ url, headers: { "X-Requested-With": "XMLHttpRequest" } })
            .done(function (html) {
                results.innerHTML = html;
                const search = document.querySelector("[data-grid-search] [name='searchString']");
                if (search) search.value = new URL(url, location.origin).searchParams.get("searchString") || "";
                if (updateHistory) history.pushState({}, "", url);
            })
            .fail(function () { showMessage("Không thể tải danh sách. Vui lòng thử lại.", "danger"); });
    }

    function searchUrl(form) {
        const url = new URL(form.action, location.origin);
        const search = new FormData(form).get("searchString")?.toString().trim();
        if (search) url.searchParams.set("searchString", search);
        else url.searchParams.delete("searchString");
        url.searchParams.set("page", "1");
        return url.toString();
    }

    function parseValidation(form) {
        if ($.validator?.unobtrusive) {
            $(form).removeData("validator").removeData("unobtrusiveValidation");
            $.validator.unobtrusive.parse(form);
        }
    }

    $(document).on("click", "[data-modal-url]", function () {
        const trigger = this;
        modalContent.innerHTML = '<div class="modal-content"><div class="modal-body text-center py-5">Đang tải...</div></div>';
        modal.show();
        $.ajax({ url: trigger.dataset.modalUrl, headers: { "X-Requested-With": "XMLHttpRequest" } })
            .done(function (html) {
                modalContent.innerHTML = html;
                parseValidation(modalContent.querySelector("form"));
            })
            .fail(function () {
                modal.hide();
                showMessage("Không thể tải biểu mẫu. Vui lòng thử lại.", "danger");
            });
    });

    $(document).on("submit", "form[data-ajax-form]", function (event) {
        event.preventDefault();
        const form = this;
        if ($.validator && $(form).data("validator") && !$(form).valid()) return;
        const submit = form.querySelector('[type="submit"]');
        if (submit) submit.disabled = true;
        $.ajax({
            url: form.action,
            method: form.method || "POST",
            data: $(form).serialize(),
            headers: { "X-Requested-With": "XMLHttpRequest" }
        }).done(function (response) {
            modal.hide();
            showMessage(response.message || "Đã lưu dữ liệu.", "success");
            loadGrid(location.href, false);
        }).fail(function (xhr) {
            if (xhr.status === 400 && xhr.responseText) {
                modalContent.innerHTML = xhr.responseText;
                parseValidation(modalContent.querySelector("form"));
            } else {
                showMessage(xhr.responseJSON?.message || "Không thể lưu dữ liệu. Vui lòng thử lại.", "danger");
                if (submit) submit.disabled = false;
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
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Hủy</button>
                    <button type="button" class="btn btn-danger" data-confirm-delete>Xóa</button>
                </div>
            </div>`;
        modalContent.querySelector("[data-delete-message]").textContent =
            `Bạn có chắc muốn xóa "${this.dataset.deleteLabel}"?`;
        modal.show();
    });

    $(document).on("click", "[data-confirm-delete]", function () {
        const form = pendingDeleteForm;
        if (!form) return;
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
            loadGrid(location.href, false);
        }).fail(function (xhr) {
            pendingDeleteForm = null;
            modal.hide();
            showMessage(xhr.responseJSON?.message || "Không thể xóa dữ liệu. Vui lòng thử lại.", "danger");
        });
    });

    $(document).on("submit", "form[data-grid-search]", function (event) {
        event.preventDefault();
        loadGrid(searchUrl(this), true);
    });

    $(document).on("click", "[data-grid-clear]", function () {
        const form = this.closest("form");
        form.querySelector("[name='searchString']").value = "";
        loadGrid(searchUrl(form), true);
    });

    $(document).on("input", "form[data-grid-search] [name='searchString']", function () {
        const form = this.form;
        clearTimeout(searchTimer);
        searchTimer = setTimeout(function () { loadGrid(searchUrl(form), true); }, 350);
    });

    $(document).on("click", "[data-grid-results] .pagination a", function (event) {
        event.preventDefault();
        if (this.closest(".page-item")?.classList.contains("disabled")) return;
        loadGrid(this.href, true);
    });

    window.addEventListener("popstate", function () { loadGrid(location.href, false); });
});
