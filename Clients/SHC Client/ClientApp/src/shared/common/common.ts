import * as _ from 'lodash';
import * as moment from 'moment';
import { AbstractControl } from '@angular/forms';

export class ValidationRule {   
    compare(frmFirst: string, codeCapcha: string) {
        return (ac: AbstractControl) => {
            if(ac.parent && ac.parent.controls[frmFirst]){
                if(ac.parent.controls[frmFirst].value != codeCapcha) return {capcha: true};
            }

            return null;
        }
    }

    //Kiem tra space, null
    hasValue(control: AbstractControl) {
        if (!control.value || (typeof control.value == 'string' && !control.value.trim())) {
            return {required: true};
        }
        return null;
    }

    //Kiem tra special character
    hasSpecialCharacter(control: AbstractControl) {
        const pattern = /^[0-9]*$/; 

        if (!pattern.test(control.value)) {
            return {special: true};
        }
        return null;
    }

    //email
    email(control: AbstractControl){
        var pattern = /^(([^<>()[\]\\.,;:\s@\"]+(\.[^<>()[\]\\.,;:\s@\"]+)*)|(\".+\"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z0-9]+\.)+[a-zA-Z]{2,}))$/;
        
        if(control.value && control.value.trim()){
            if (!pattern.test(control.value)) {
                return {email: true};
            }
    
            if (control.value.split('@')[0].length > 64 || control.value.split('@')[1].length > 255) {
                return {email: true};
            }
        }
        
        return null;
    }

    //phone
    topPhoneNumber(control: AbstractControl) {
        let arr: string[] = ['086', '096', '097', '098', '032', '033', '034', '035', '036', '037', '038', '039',
            '089', '090', '093', '070', '079', '077', '076', '078',
            '091', '094', '088', '083', '084', '085', '081', '082',
            '092', '056', '058', '099', '059'];
        let arr84: string[] = ['8486', '8496', '8497', '8498', '8432', '8433', '8434', '8435', '8436', '8437', '8438', '8439',
            '8489', '8490', '8493', '8470', '8479', '8477', '8476', '8478',
            '8491', '8494', '8488', '8483', '8484', '8485', '8481', '8482',
            '8492', '8456', '8458', '8499', '8459'];
        const pattern = /^[0-9\+]*$/;
        if (control.value && control.value != "") {
            if (!pattern.test(control.value)) {
                control.setValue(control.value.replace(/[^0-9\+]/g, ""));
            }
            if (control.value.indexOf("84") == 0 || control.value.indexOf("84") == 1) {
                if (control.value.charAt(0) != '+' && control.value.length >= 4 && control.value.indexOf("84") == 0 && (arr84.indexOf(control.value.substring(0, 4)) < 0)) {
                    return { topnumber: true };
                }
                if (control.value.charAt(0) == '+' && control.value.length >= 5 && control.value.indexOf("84") == 1 && (arr84.indexOf(control.value.substring(1, 5)) < 0)) {
                    return { topnumber: true };
                }
            }
            else {
                if (control.value.charAt(0) != '+' && control.value.length >= 3 && (arr.indexOf(control.value.substring(0, 3)) < 0)) {
                    return { topnumber: true };
                }
                if (control.value.charAt(0) == '+' && control.value.length >= 4 && (arr.indexOf(control.value.substring(1, 4)) < 0)) {
                    return { topnumber: true };
                }
            }
            if (control.value.charAt(0) != '+' && control.value.indexOf("+") > 0) {
                return { invalidphonenumber: true };
            }
            var first = control.value.indexOf("+");
            var second = control.value.lastIndexOf("+");
            if (first != second) {
                return { invalidphonenumber: true };
            }
            if (control.value.length < 10) {
                return { minlength: true };
            }
        }
        return null;
    }

    //phone
    identification(control: AbstractControl){
        if (control.value && !(control.value.length == 9 || control.value.length == 12)) {
            return {identification: true};
        }
        
        return null;
    }

    passwordStrong(control: AbstractControl){
        var pattern = /^(?=.*[0-9])(?=.*[`~!@#$%^&*()_\-+={\[}\]|\\:;'"<,>.?/])[a-zA-Z0-9`~!@#$%^&*()_\-+={\[}\]|\\:;'"<,>.?/]{8,255}$/;
        if(control.value.length >= 8)
        {
            if(control.value && !pattern.test(control.value)){
                return {passwordStrong: true};
                
            }    
            if(control.value && control.value.search(/[A-Z]/) < 0){
                return {passwordStrong: true};            
            }
        }       
        return null;
    }
    passwordValidate(control: AbstractControl) {
        var pattern = /^(?=.*[0-9])(?=.*[`~!@#$%^&*()_\-+={\[}\]|\\:;'"<,>.?/])[a-zA-Z0-9`~!@#$%^&*()_\-+={\[}\]|\\:;'"<,>.?/]{8,255}$/;
        if (control.value.length >= 8) {
            if (control.value && !pattern.test(control.value)) {
                return { passwordValidate: true };

            }
            if (control.value && control.value.search(/[A-Z]/) < 0) {
                return { passwordValidate: true };
            }
        }
        return null;
    }
    //Date valid
    dateInvalid(control: AbstractControl){
        if(control.value && !moment(control.value, 'DD/MM/YYYY').isValid()){
            return {special: true};
        }
        return null;
    }

    // Checkbox
    checkBox(control: AbstractControl) {
        if (!control.value) {
            return { checkBox: true };
        }
        return null;
    }
}
