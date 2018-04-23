Date.prototype.toUTCString = function () {

    function zeroCompletion(time) {
        return ("00" + time).slice(-2);
    }
    return this.getFullYear() + "-" +
        zeroCompletion(this.getMonth() + 1) + "-" +
        zeroCompletion(this.getDate()) + "T" +
        zeroCompletion(this.getHours()) + ":" +
        zeroCompletion(this.getMinutes()) + ":" +
        zeroCompletion(this.getSeconds())
}

if (!String.prototype.format) {
    String.prototype.format = function () {
        var args = arguments;
        return this.replace(/{(\d+)}/g, function (match, number) {
            return typeof args[number] != 'undefined'
                ? args[number]
                : match
                ;
        });
    };
}

function transDateTime(time) {
    var diff = Math.round(new Date().getTime() / 1000) - Math.round(new Date(time).getTime() / 1000);
    if (diff < 60) {
        return "刚刚";
    }
    else if (diff > 60 && diff < 3600) {
        return "{0}分钟前".format(Math.round(diff / 60));
    }
    else if (diff > 3600 && diff < 3600 * 24) {
        return "{0}小时前".format(Math.round(diff / 3600));
    }
    else if (diff > 3600 * 24 && diff < 3600 * 24 * 30) {
        return "{0}天前".format(Math.round(diff / 3600 / 24));
    }
    else if (diff > 3600 * 24 * 30 && diff < 3600 * 24 * 30 * 12) {
        return "{0}月前".format(Math.round(diff / 3600 / 24 / 30));
    }
    else if (diff > 3600 * 24 * 30 * 12) {
        return "{0}年前".format(Math.round(diff / 3600 / 24 / 30 / 12));
    }
}

Object.defineProperty(Date, "timeZone", {
    get: function () {
        var hourOffset = parseInt(new Date().getTimezoneOffset() / 60);
        return "Etc/GMT" +
            (hourOffset > 0 ? "+" + hourOffset :
                hourOffset == 0 ? "" :
                    "-" + Math.abs(hourOffset));
    }
})

var graph;

String.prototype.endsWith = function (pattern) {
    var d = this.length - pattern.length;
    return d >= 0 && this.lastIndexOf(pattern) === d;
};

$.graph.prototype.GetUser = function () {
    //https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_get

    return common.Request.call(
        this,
        "https://graph.microsoft.com/v1.0/me",
        {
            request_header: {
                'Authorization': 'Bearer ' + window.sessionStorage.token,
                "Content-Type": "application/json",
                "Prefer": 'outlook.timezone="' + Date.timeZone + '"'
            },
            request_body: {}
        },
        "GET",
        true);
}

$.graph.prototype.GetFileList = function (prefixUrl) {
    //https://docs.microsoft.com/en-us/onedrive/developer/rest-api/api/driveitem_list_children

    var url = prefixUrl + "/children?select=name,id,webUrl,@microsoft.graph.downloadUrl,createdDateTime,folder,parentReference";
    return common.Request.call(
        this,
        url,
        {
            request_header: {
                'Authorization': 'Bearer ' + window.sessionStorage.token,
                "Content-Type": "application/json",
                "Prefer": 'outlook.timezone="' + Date.timeZone + '"'
            },
            request_body: {}
        },
        "GET",
        true);
}

function GetMyProfile() {
    return graph.GetUser().then(function (that) {
        var data = that.res;

        var container = $(".header");
        if (data) {
            container.find("label").text(data.displayName || data.userPrincipalName + "/");
            container.find("a").text("switch user");
        } else {
            container.find("label").text();
            container.find("a").text("login");
        }

        return data;
    });
}

function GetFiles(prefixUrl, array) {
    return graph.GetFileList(prefixUrl).then(function (that) {
        var data = that.res;
        $.each(data.value, function (i, item) {
            if (item["@microsoft.graph.downloadUrl"] &&
                (item.name.endsWith(".pptx") || item.name.endsWith(".docx") || item.name.endsWith(".xlsx"))) {
                var object = {
                    Id:item.id,
                    Name: item.name,
                    DownloadPath: item["@microsoft.graph.downloadUrl"],
                    Path: item.parentReference.path,
                    CreatedDateTime: item.createdDateTime
                };
                array.push(object);
            }
            else if (item.folder && item.folder.childCount > 0) {
                prefixUrl = "https://graph.microsoft.com/v1.0/me" + item.parentReference.path + "/" + item.name + ":"
                GetFiles(prefixUrl, array);
            }
        })
    });
}

function GetRawFiles(prefixUrl, array) {
    return graph.GetFileList(prefixUrl).then(function (that) {
        var data = that.res;
        $.each(data.value, function (i, item) {
            if (item["@microsoft.graph.downloadUrl"] && item.name.endsWith(".rawdata")) {
                var object = {
                    Name: item.name,
                    DownloadPath: item["@microsoft.graph.downloadUrl"],
                    CreatedDateTime: item.createdDateTime
                };
                array.push(object);
            }
        })
    });
}