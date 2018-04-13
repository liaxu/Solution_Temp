Office.initialize = function (reason) {
    $(document).ready(function () {
        if (window.location.search && window.location.search.indexOf("?code=") == 0) {
            Office.context.ui.messageParent(window.location.search.substr(1));
        }
    });
}