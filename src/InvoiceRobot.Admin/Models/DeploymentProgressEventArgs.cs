namespace InvoiceRobot.Admin.Models;

/// <summary>
/// Event args for deployment progress updates
/// </summary>
public class DeploymentProgressEventArgs : EventArgs
{
    public int PercentComplete { get; set; }
    public string Message { get; set; } = string.Empty;
}
