app.ready(function () {
    $("#getTable").click(function () {
        app.getTable(function (result) {
            $("#log").append(JSON.stringify(result));
        })
    })
})