using System.Windows;
using FlorrTeach.Services;

namespace FlorrTeach.Views;

public partial class ModulePanelWindow : Window
{
    private readonly FloatingBallWindow _ball;

    public ModulePanelWindow(FloatingBallWindow ball)
    {
        InitializeComponent();
        WindowExtras.MarkAsToolWindow(this);
        _ball = ball;
    }
}
