import { Routes } from '@angular/router';
import { authGuard } from './auth.guard';
import { Login } from './login/login';
import { Articles } from './articles/articles';

export const routes: Routes = [
  { path: 'login', component: Login },
  { path: '', component: Articles, canActivate: [authGuard] },
  { path: '**', redirectTo: '' }
];
