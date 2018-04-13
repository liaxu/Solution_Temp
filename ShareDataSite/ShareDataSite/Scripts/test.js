//var base64
//$(document).ready(function () {
//    $("#image img").load(function (e) {
//        e
//    })
//})
app.ready(function () {
    $("#text").click(function () {
        app.insertText($(this).text());
    })
    $("#image").click(function () {
        var img = new Image();
        //app.insertImage(base64);
        var src = $(this).find("img").attr("src");
        function getBase64Image(img) {
            var canvas = document.createElement("canvas");
            canvas.width = img.width;
            canvas.height = img.height;
            var ctx = canvas.getContext("2d");
            ctx.drawImage(img, 0, 0, img.width, img.height);
            var dataURL = canvas.toDataURL("image/png");
            return dataURL // return dataURL.replace("data:image/png;base64,", ""); 
        }
        function main() {
            var img = document.createElement('img');
            img.src = src;
            img.setAttribute('crossOrigin', 'anonymous');
            img.onload = function () {
                var data = getBase64Image(img);
                console.log(data);
                data = data.substring(data.indexOf(",") + 1);
                app.insertImage(data);
            }
            document.body.appendChild(img);
            img.hidden = true;
        }
        main();
    })

    $("#html").click(function () {
        app.insertHtml($(this).children("div").get(0).outerHTML);
    })
})