﻿
namespace Chat.Core.ViewModel;

public partial class LoginViewModel : BaseViewModel
{
    [ICommand]
    private async void Login()
    {
       await Shell.Current.GoToAsync("//Tabs");
    }

}