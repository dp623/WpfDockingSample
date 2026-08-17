using System;
using System.Windows.Input;

namespace WpfDockingSample;

/// <summary>デリゲートをICommandとして公開します。</summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    /// <summary>コマンドを初期化します。</summary>
    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>コマンドを実行できるか判定します。</summary>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    /// <summary>コマンドを実行します。</summary>
    public void Execute(object? parameter) => _execute(parameter);

    /// <summary>実行可否状態が変化したときに発生します。</summary>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
