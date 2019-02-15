
var Configuration = function (type) {
    this.name = '/Configuration.svc';
    this.type = type ? type : 'GET';
    this.GetSystemUsers = function (p, onResult, onError) { return this.implement('GetSystemUsers', p, onResult, onError); };
    this.GetSystemUsers.params = {};
    this.GetSystemGroups = function (p, onResult, onError) { return this.implement('GetSystemGroups', p, onResult, onError); };
    this.GetSystemGroups.params = {};
    this.GetApplications = function (p, onResult, onError) { return this.implement('GetApplications', p, onResult, onError); };
    this.GetApplications.params = {};
    this.GetHosts = function (p, onResult, onError) { return this.implement('GetHosts', p, onResult, onError); };
    this.GetHosts.params = {};
    this.GetSystemInfo = function (p, onResult, onError) { return this.implement('GetSystemInfo', p, onResult, onError); };
    this.GetSystemInfo.params = {};
    this.GetLicenseInfo = function (p, onResult, onError) { return this.implement('GetLicenseInfo', p, onResult, onError); };
    this.GetLicenseInfo.params = {};
    this.RegisterLicense = function (p, onResult, onError) { return this.implement('RegisterLicense', p, onResult, onError); };
    this.RegisterLicense.params = { lic: '' };
    this.GetConfigurations = function (p, onResult, onError) { return this.implement('GetConfigurations', p, onResult, onError); };
    this.GetConfigurations.params = {};
    this.SetConfigurations = function (p, onResult, onError) { return this.implement('SetConfigurations', p, onResult, onError); };
    this.SetConfigurations.params = { prams: '' };
    this.SetAdminPassword = function (p, onResult, onError) { return this.implement('SetAdminPassword', p, onResult, onError); };
    this.SetAdminPassword.params = { pwd: '' };

};
Configuration.prototype =
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