import * as _ from 'lodash';
import { AbstractControl } from '@angular/forms';
import { DatePipe } from '@angular/common';

export class ValidationRule {
    //So sanh 2 control
    compare(frmFirst: string, frmSecond: string) {
        return (ac: AbstractControl) => {
            if(ac.parent && ac.parent.controls[frmFirst] && ac.parent.controls[frmSecond]){
                if(Number(ac.parent.controls[frmFirst].value) >= Number(ac.parent.controls[frmSecond].value)) return {compare: true};
                //ac.parent.controls[frmSecond].errors = {compare: true}
            }

            return null;
        }
    }


    //Kiem tra space, null
    hasValue(control: AbstractControl) {
        if ((!control.value || (typeof control.value == 'string' && !control.value.trim())) && !isNaN(control.value)) {
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
}
