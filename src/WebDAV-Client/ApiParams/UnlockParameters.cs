using System.Threading;
using Stef.Validation;

namespace WebDav;

/// <summary>
/// Represents parameters for the UNLOCK WebDAV method.
/// </summary>
public class UnlockParameters
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnlockParameters"/> class.
    /// <param name="lockToken">The resource lock token.</param>
    /// </summary>
    public UnlockParameters(string lockToken)
    {
        LockToken = Guard.NotNull(lockToken);
        CancellationToken = CancellationToken.None;
    }

    /// <summary>
    /// Gets the resource lock token.
    /// </summary>
    public string LockToken { get; }

    /// <summary>
    /// Gets or sets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }
}