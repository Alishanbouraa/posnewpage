using QuickTechPOS.Helpers;
using QuickTechPOS.Services;
using QuickTechPOS.ViewModels;
using System.Windows.Input;

public class OpenDrawerViewModel : BaseViewModel
{
    private readonly DrawerService _drawerService;
    private readonly AuthenticationService _authService;

    private decimal _openingBalance;
    private string _errorMessage;
    private bool _isProcessing;
    private string _notes;

    public decimal OpeningBalance
    {
        get => _openingBalance;
        set => SetProperty(ref _openingBalance, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        set => SetProperty(ref _isProcessing, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public ICommand OpenDrawerCommand { get; }
    public ICommand CancelCommand { get; }

    public OpenDrawerViewModel(AuthenticationService authService)
    {
        _drawerService = new DrawerService();
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));

        OpeningBalance = 0;
        ErrorMessage = string.Empty;
        IsProcessing = false;
        Notes = string.Empty;

        OpenDrawerCommand = new RelayCommand(async param => await OpenDrawerAsync(), CanOpenDrawer);
        CancelCommand = new RelayCommand(param => Cancel());
    }

    private bool CanOpenDrawer(object parameter)
    {
        return !IsProcessing && OpeningBalance >= 0;
    }

    private async Task OpenDrawerAsync()
    {
        try
        {
            IsProcessing = true;
            ErrorMessage = string.Empty;

            if (OpeningBalance < 0)
            {
                ErrorMessage = "Opening balance cannot be negative.";
                IsProcessing = false;
                return;
            }

            var employee = _authService.CurrentEmployee;
            if (employee == null)
            {
                ErrorMessage = "No cashier is logged in.";
                IsProcessing = false;
                return;
            }

            string cashierId = employee.EmployeeId.ToString();
            string cashierName = employee.FullName;

            // Open the drawer
            var drawer = await _drawerService.OpenDrawerAsync(cashierId, cashierName, OpeningBalance, Notes);

            if (drawer != null)
            {
                DialogResult = true;
            }
            else
            {
                ErrorMessage = "Failed to open drawer. Please try again.";
                DialogResult = false;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error opening drawer: {ex.Message}";
            DialogResult = false;
        }
        finally
        {
            IsProcessing = false;
            CommandManager.InvalidateRequerySuggested();
            OnPropertyChanged(nameof(DialogResult));
        }
    }

    private void Cancel()
    {
        DialogResult = false;
        OnPropertyChanged(nameof(DialogResult));
    }

    public bool? DialogResult { get; private set; }
}