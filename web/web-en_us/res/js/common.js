/*-----------------------------------------------------------------------------
    common functions for TSCamera
-----------------------------------------------------------------------------*/

if ($.fn.datebox) {
    $.fn.datebox.defaults.formatter = function (date) {
        var y = date.getFullYear();
        var m = date.getMonth() + 1;
        var d = date.getDate();
        return y + '-' + (m < 10 ? ('0' + m) : m) + '-' + (d < 10 ? ('0' + d) : d);
    };
}

function date_value(days) {
    var date = new Date();
    if (days)
        date.setDate(date.getDate() + days);
    var y = date.getFullYear();
    var m = date.getMonth() + 1;
    var d = date.getDate();
    return y + '-' + (m < 10 ? ('0' + m) : m) + '-' + (d < 10 ? ('0' + d) : d);
}

function url_parameters() {
    var prams = {};
    var segments = window.location.href.split('?');
    if (segments.length > 1) {
        var ps = segments[1].split('&');
        $.each(ps, function (k, v) {
            var ss = this.split('=');
            if (ss.length > 0) {
                prams[ss[0]] = unescape(ss[1]);
            }
        });
    }
    return prams;
}

function bold_format(val, txt) {
    if (val && val.length > 0 && txt && txt.length > 0) {
        var v = escape(val);
        var t = escape(txt).replace(/\//g, '\\/').replace(/\*/g, '\\*');
        try {
            val = unescape(eval("v.replace(/" + t + "/ig, '<b>$&</b>')"));
        }
        catch (err) { }
        val = val.replace(/\r\n/g, '<br/>');
    }
    return val;
}

function str_compare(a, b) {
    a = (a + '').toLowerCase();
    b = (b + '').toLowerCase();
    return a.localeCompare(b);
}
