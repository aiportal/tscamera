
var Statistic = function (type) {
    this.name = '/Statistic.svc';
    this.type = type ? type : 'GET';
    this.GetUsers = function (p, onResult, onError) { return this.implement('GetUsers', p, onResult, onError); };
    this.GetUsers.params = {};
    this.ComputerUsage = function (p, onResult, onError) { return this.implement('ComputerUsage', p, onResult, onError); };
    this.ComputerUsage.params = { start: '', end: '', user: '' };
    this.ProgramUsage = function (p, onResult, onError) { return this.implement('ProgramUsage', p, onResult, onError); };
    this.ProgramUsage.params = { start: '', end: '', user: '' };
    this.HostVisit = function (p, onResult, onError) { return this.implement('HostVisit', p, onResult, onError); };
    this.HostVisit.params = { start: '', end: '', user: '' };

};
Statistic.prototype =
{
    ajaxInvoke: function (url, prams, onResult, onError) {
        $.support.cors = true;
        $.ajax({
            url: url,
            type: this.type,
            data: prams,
            cache: false,
            dataType: 'json',
            success: function (data) {
                onResult(data);
            },
            error: function (request) {
                if (onError)
                    onError(request.responseText);
            }
        });
    },
    implement: function (method, prams, onResult, onError) {
        var url = 'http://' + location.host + this.name + '?$m=' + method;
        url = url.replace(':1066', ':99');
        for (var attr in prams)
            prams[attr] = prams[attr] ? escape(prams[attr]) : prams[attr];
        if (onResult) {
            this.ajaxInvoke(url, prams, onResult, onError);
        }
        else {
            for (var attr in prams)
                url += prams[attr] ? '&' + attr + '=' + prams[attr] : '';
            return url;
        }
    }
};