﻿#if NETSTANDARD || PORTABLE
// Copied from https://github.com/Microsoft/referencesource/blob/4fe4349175f4c5091d972a7e56ea12012f1e7170/System/compmod/system/componentmodel/InvalidEnumArgumentException.cs

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>The exception that is thrown when using invalid arguments that are enumerators.</para>
    /// </devdoc>
    public class InvalidEnumArgumentException : ArgumentException
    {
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.InvalidEnumArgumentException'/> class without a message.</para>
        /// </devdoc>
        public InvalidEnumArgumentException() : this(null)
        {
        }

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.InvalidEnumArgumentException'/> class with 
        ///    the specified message.</para>
        /// </devdoc>
        public InvalidEnumArgumentException(string message)
            : base(message)
        {
        }

        /// <devdoc>
        ///     Initializes a new instance of the Exception class with a specified error message and a 
        ///     reference to the inner exception that is the cause of this exception.
        ///     FxCop CA1032: Multiple constructors are required to correctly implement a custom exception.
        /// </devdoc>
        public InvalidEnumArgumentException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.InvalidEnumArgumentException'/> class with a 
        ///    message generated from the argument, invalid value, and enumeration
        ///    class.</para>
        /// </devdoc>
        public InvalidEnumArgumentException(string argumentName, int invalidValue, Type enumClass)
            : base($"The value of argument '{argumentName}' ({invalidValue}) is invalid for Enum type '{enumClass.Name}'", argumentName)
        {
        }
    }
}
#endif