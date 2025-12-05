import { createApp } from "vue";
import PrimeVue from 'primevue/config';
import 'primeicons/primeicons.css';
import 'bootstrap/dist/css/bootstrap.min.css';
import Index from './Index.vue';
import Aura from '@primeuix/themes/aura';
import {ToastService, ConfirmationService} from "primevue";
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Paginator from 'primevue/paginator';
import ConfirmDialog from 'primevue/confirmdialog';
import Toast from 'primevue/toast';
import Select from 'primevue/select';
import MultiSelect from 'primevue/multiselect';
import ToggleSwitch from 'primevue/toggleswitch';
import TreeSelect from 'primevue/treeselect';
import store from './store';

const app = createApp(Index);
app.use(PrimeVue, {
    theme: {
        preset: Aura,
        options: {
            darkModeSelector: false,
        }
    },
});
app.use(ToastService);
app.use(ConfirmationService);
app.use(store);
app.component('Button', Button);
app.component('Dialog', Dialog);
app.component('InputText', InputText);
app.component('DataTable', DataTable);
app.component('Column', Column);
app.component('Paginator', Paginator);
app.component('ConfirmDialog', ConfirmDialog);
app.component('Toast', Toast);
app.component('Select', Select);
app.component('MultiSelect', MultiSelect);
app.component('ToggleSwitch', ToggleSwitch);
app.component('TreeSelect', TreeSelect);
app.mount("#app");