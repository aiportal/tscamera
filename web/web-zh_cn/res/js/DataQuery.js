
var DataQuery = function (type) {
    this.name = '/DataQuery.svc';
    this.type = type ? type : 'GET';
    this.GetUsers = function (p, onResult, onError) { return this.implement('GetUsers', p, onResult, onError); };
    this.GetUsers.params = {};
    this.GetApplications = function (p, onResult, onError) { return this.implement('GetApplications', p, onResult, onError); };
    this.GetApplications.params = {};
    this.GetHosts = function (p, onResult, onError) { return this.implement('GetHosts', p, onResult, onError); };
    this.GetHosts.params = {};
    this.GetDrives = function (p, onResult, onError) { return this.implement('GetDrives', p, onResult, onError); };
    this.GetDrives.params = {};
    this.GetSessions = function (p, onResult, onError) { return this.implement('GetSessions', p, onResult, onError); };
    this.GetSessions.params = { start: '', end: '', user: '' };
    this.GetSnapshots = function (p, onResult, onError) { return this.implement('GetSnapshots', p, onResult, onError); };
    this.GetSnapshots.params = { sid: '' };
    this.SnapshotsByTitle = function (p, onResult, onError) { return this.implement('SnapshotsByTitle', p, onResult, onError); };
    this.SnapshotsByTitle.params = { sid: '', prog: '', title: '' };
    this.SnapshotsByUrl = function (p, onResult, onError) { return this.implement('SnapshotsByUrl', p, onResult, onError); };
    this.SnapshotsByUrl.params = { sid: '', host: '', url: '' };
    this.SearchTitle = function (p, onResult, onError) { return this.implement('SearchTitle', p, onResult, onError); };
    this.SearchTitle.params = { start: '', end: '', user: '', prog: '', title: '' };
    this.SearchText = function (p, onResult, onError) { return this.implement('SearchText', p, onResult, onError); };
    this.SearchText.params = { start: '', end: '', user: '', prog: '', text: '' };
    this.SearchUrl = function (p, onResult, onError) { return this.implement('SearchUrl', p, onResult, onError); };
    this.SearchUrl.params = { start: '', end: '', user: '', host: '', url: '' };
    this.SearchFile = function (p, onResult, onError) { return this.implement('SearchFile', p, onResult, onError); };
    this.SearchFile.params = { start: '', end: '', user: '', drive: '', file: '' };
    this.GetImage = function (p, onResult, onError) { return this.implement('GetImage', p, onResult, onError); };
    this.GetImage.params = { Response: '', ssid: '' };

};
DataQuery.prototype =
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