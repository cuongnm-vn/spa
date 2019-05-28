export class AppConsts {

    static remoteServiceBaseUrl: string;
    static serverBaseUrl: string; 

    static uploadBaseUrl: string;

    static appBaseUrl: string;
    static appBaseHref: string; // returns angular's base-href parameter value if used during the publish

    static appId: number;
    static appName: string;

    static localeMappings: any = [];

    static readonly userManagement = {
        defaultAdminUserName: 'admin'
    };

    static readonly localization = {
        defaultLocalizationSourceName: 'viettel'
    };

    static readonly authorization = {
        encrptedAuthTokenName: 'enc_auth_token'
    };
}
