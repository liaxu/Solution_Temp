"use strict";
!function () {
    var app = window.app = {};
    var onready = app.onready = [];
    app.ready = function (func) {//called when login success
        onready.push(func);
    }
}()

Office.initialize = function () {
    var app = window.app;

    $(document).ready(function () {

        $.graph.login(function (res) {
            if (res) {
                app.onready.map(function (func) {
                    func();
                })
            }
        });

        app.insertImage = function (base64, callback) {
            Office.context.document.setSelectedDataAsync(base64, {
                coercionType: Office.CoercionType.Image,
            }, function (asyncResult) {
                callback && callback(asyncResult);
            });
        };
        app.insertText = function (text, callback) {
            Office.context.document.setSelectedDataAsync(text, {
                coercionType: Office.CoercionType.Text,
            }, function (asyncResult) {
                callback && callback(asyncResult);
            });
        };

        app.insertHtml = function (html, callback) {
            Office.context.document.setSelectedDataAsync(html, {
                coercionType: Office.CoercionType.Html,
            }, function (asyncResult) {
                callback && callback(asyncResult);
            });
        };

        app.insertTable = function (tableBody, tableHeader, callback) {
            var table = new Office.TableData();
            if (tableHeader && tableHeader.length) {
                table.headers = [tableHeader];
            }
            table.rows = tableBody;

            Office.context.document.setSelectedDataAsync(table, {
                coercionType: Office.CoercionType.Table,
            }, function (asyncResult) {
                callback && callback(asyncResult);
            });
        };

        if (Office.context.requirements.isSetSupported("ExcelApi")) {
            app.insertTable = function (tableBody, tableHeader) {
                Excel.run(function (context) {
                    const range = context.workbook.getSelectedRange();
                    range.load("address");

                    var tableWidth = tableBody ? tableBody.length ? tableBody[0].length : 0 : 0;

                    function convert26BSToDS(code) {
                        var num = -1;
                        var reg = /^[A-Z]+$/g;
                        if (!reg.test(code)) {
                            return num;
                        }
                        num = 0;
                        for (var i = code.length - 1, j = 1; i >= 0; i-- , j *= 26) {
                            num += (code[i].charCodeAt() - 64) * j;
                        }
                        return num;
                    }

                    function convertDSTo26BS(num) {
                        var code = '';
                        var reg = /^\d+$/g;
                        if (!reg.test(num)) {
                            return code;
                        }
                        while (num > 0) {
                            var m = num % 26
                            if (m == 0) {
                                m = 26;
                            }
                            code = String.fromCharCode(64 + parseInt(m)) + code;
                            num = (num - m) / 26;
                        }
                        return code;
                    }

                    return context.sync().then(function () {
                        var address = function () {
                            var address = range.address;
                            var exclamationMark = range.address.lastIndexOf("!"), colon = address.lastIndexOf(":");
                            var start, end, row;
                            var tempStart = address.substring(exclamationMark + 1, colon == -1 ? address.length : colon);
                            var firstDigit = tempStart.match(/\d/);
                            var indexed = tempStart.indexOf(firstDigit);
                            row = parseInt(tempStart.substr(indexed));
                            start = tempStart.substr(0, indexed);
                            end = convert26BSToDS(start) + tableWidth - 1;
                            if (colon == -1) {
                                return address + ":" + convertDSTo26BS(end) + row;
                            } else {
                                return address.substring(0, colon) + ":" + convertDSTo26BS(end) + row;
                            }
                        }()
                        const currentWorksheet = context.workbook.worksheets.getActiveWorksheet();
                        //const expensesTable = currentWorksheet.tables.add(range, tableHeader && true /*hasHeaders*/);
                        const expensesTable = currentWorksheet.tables.add(address, !!tableHeader.length);

                        if (tableHeader.length) {
                            expensesTable.getHeaderRowRange().values =
                                [tableHeader];
                        }
                        expensesTable.rows.add(null /*add at the end*/, tableBody);
                    });
                })
                    .catch(function (error) {
                        console.log("Error: " + error);
                        if (error instanceof OfficeExtension.Error) {
                            app.dialog(error.name, error.message)
                            console.log("Debug info: " + JSON.stringify(error.debugInfo));
                        }
                    });
            }
        }

        if (Office.context.requirements.isSetSupported("WordApi")) {

        }

        if (Office.context.requirements.isSetSupported("PowerPointApi")) {

        }

        app.dialog = function (title, content) {
            var dialog = $("#ShareDatadialog");
            dialog.find(".ShareDatadialog-title").text(title);
            dialog.find(".ShareDatadialog-content").text(content);
            dialog.slideDown();
            setTimeout(function () {
                dialog.slideUp();
            }, 2000);
        }
    })
}


$.hashParam = function (hashstr) {
    var hash = hashstr.split("&");
    var params = {}
    for (var i = 0; i < hash.length; i++) {
        var split = hash[i].indexOf("=");
        params[hash[i].substring(0, split)] = hash[i].substring(split + 1);
    }
    return params;
}

$.graph = function (setting) {
    this.setting = setting;
}

$.graph.prototype.login = function (token, authorization, expire_time) {
    if (token && authorization) {
        var expire_time = new Date();
        expire_time.setSeconds(expire_time.getSeconds() + authorization.expires_in);
        expire_time = expire_time.toUTCString();
        authorization = JSON.stringify(authorization);
    } else {
        if (window.sessionStorage.authorization) {
            authorization = window.sessionStorage.authorization;
            token = JSON.parse(window.sessionStorage.authorization).access_token;
            expire_time = window.sessionStorage.expire_time;
        } else {
            console.info("sessionStorage.authorization undefined. login failed.");
            return false;
        }
    }
    this.token = window.sessionStorage.token = token;
    this.authorization = authorization;
    this.expire_time = window.sessionStorage.expire_time = expire_time;
    setTimeout(this.refreshToken.bind(this), function () {
        var span = new Date(this.expire_time) - new Date();
        return ((span - 1000000) < 0 ? 0 : (span - 1000000));
    }.bind(this)());
    return true;
}

$.graph.prototype.refreshToken = function () {
    var that = this;
    if (!(this.authorization && this.authorization.refresh_token))
        throw "no authorization or refresh_token set";

    $.ajax({
        url: "/Authorization/RefreshToken",
        data: { refresh_token: this.authorization.refresh_token },
        type: 'POST',
        success: function (res) {
            that.login(res.access_token, res);
        },
        error: function (err) {
            console.error("refreshToken failed");
            console.error(err);
        }
    })
}


var common = function () {
    //get row number and file from stack
    function codeRowNum(depth) {
        if (!depth)
            depth = 1;
        try {
            throw new Error();
        } catch (e) {
            var stack = e.stack.substring(5).replace(/[\r\n]/i, "").split(/[\r\n]/g);
            var codeRow = stack[depth];
            return codeRow.substring(codeRow.lastIndexOf("/") + 1, codeRow.lastIndexOf(":"));
        }
    }

    function response(res, resStatus, resPromiseObj, isLogin) {
        //if requeset is error and response data is different from the succee
        if (resStatus == 'error') {
            var temp = res;
            res = res.responseText;
            resPromiseObj = temp;
        }

        this.res = this.response = res;

        if (!isLogin) {
            var headerArr = resPromiseObj.getAllResponseHeaders().trim().split(/[\r\n]+/);
            var headerObj = {};
            headerArr.forEach(function (line) {
                var parts = line.split(': ');
                var header = parts.shift();
                var value = parts.join(': ');
                headerObj[header] = value;
            });

            vm.response.response_body = res;
            vm.response.response_header = headerObj;
        }

        this.status = resStatus;
    }

    function request(url, data, method, isLogin) {
        var that = this;
        var stack = codeRowNum(3);

        if (!method && typeof data === "string")
            method = data, data = null;

        var promise = new Promise(function (resolve, reject) {
            var option = {
                url: url,
                headers: typeof data.request_header == 'object' ? data.request_header : JSON.parse(data.request_header),
                method: method,
                success: function (res, resStatus, resPromiseObj) {
                    var callResponse = response.bind(this, res, resStatus, resPromiseObj, isLogin);
                    callResponse()
                    resolve(this);
                },
                error: function (res, resStatus, resPromiseObj) {
                    var callResponse = response.bind(this, res, resStatus, resPromiseObj, isLogin);
                    callResponse()
                    reject(this);
                }
            };
            option.context = {
                url: option.url,
                method: option.method,
                codeSituation: stack,
                data: {}
            }
            if (data && data.request_body) {
                option.data = typeof data.request_body === 'object' ? data.request_body : JSON.parse(data.request_body);
                option.context.data = option.data;
            }
            $.ajax(option);
        })
        promise.catch(function (ajax) {
            var err = ajax.res;
            console.info(err.status + " " + err.statusText)
            console.info(err.responseText)
        });

        return promise;
    }

    $.post = function post(url, data) {
        if (typeof data === "object")//used to crossDomain
            data = JSON.stringify(data);
        return request.call(this, url, data, "POST");
    }

    $.get = function get(url, data) {
        return request.call(this, url, data, "GET");
    }

    $.patch = function patch(url, data) {
        if (typeof data === "object")//used to crossDomain
            data = JSON.stringify(data);
        return request.call(this, url, data, "PATCH");
    }

    $.del = function del(url, data) {
        if (typeof data === "object")//used to crossDomain
            data = JSON.stringify(data);
        return request.call(this, url, data, "DELETE");
    }

    return {
        Request: request
    };
}()

$.graph.login = function (setting) {
    var graph = window.graph = new $.graph(setting);
    var _dlg;

    return function (callback) {
        //初始化graph实例

        if (graph.login()) {
            callback(true);
        } else {
            Office.context.ui.displayDialogAsync(
                location.protocol + '//' + location.hostname + (location.port ? ':' + location.port : '') + "/Authorization/Login",
                { height: 80, width: 50 },
                function (result) {
                    _dlg = result.value;
                    _dlg.addEventHandler(Microsoft.Office.WebExtension.EventType.DialogMessageReceived, function (msg) {
                        var authorization = $.hashParam(msg.message);
                        $.ajax({
                            url: "/Authorization/Code",
                            data: {
                                "code": authorization.code,
                            },
                            type: 'POST',
                            success: function (data) {
                                var access_token;
                                if ((data instanceof Object)) {
                                    access_token = data.access_token;
                                } else {
                                    access_token = data.getParam("access_token");
                                }
                                if (graph.login(access_token, data)) {
                                    callback(access_token, data);
                                }
                            },
                            error: function (error) {
                                console.error(error);
                            }
                        });
                        console.log(msg);
                        _dlg.close();
                    });
                });
        }
    }
}(setting)
