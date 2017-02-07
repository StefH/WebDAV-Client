﻿namespace WebDav
{
    /// <summary>
    /// Represents a lock owner identified by principal name.
    /// </summary>
    public class PrincipalLockOwner : LockOwner
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrincipalLockOwner"/> class.
        /// </summary>
        /// <param name="principalName">Name of the principal.</param>
        public PrincipalLockOwner(string principalName)
        {
            Check.NotNull(principalName, nameof(principalName));
            Value = principalName;
        }

        /// <summary>
        /// Gets a value representing an owner.
        /// </summary>
        public override string Value { get; }
    }
}
