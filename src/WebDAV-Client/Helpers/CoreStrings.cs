using System;
using JetBrains.Annotations;

// copied from https://github.com/aspnet/EntityFramework/blob/dev/src/Microsoft.EntityFrameworkCore/Properties/CoreStrings.resx
namespace WebDav
{
    internal static class CoreStrings
    {
        /// <summary>
        /// The property '{property}' of the argument '{argument}' cannot be null.
        /// </summary>
        public static string ArgumentPropertyNull([CanBeNull] string property, [CanBeNull] string argument)
        {
            return string.Format($"The property '{property}' of the argument '{argument}' cannot be null.", property, argument);
        }

        /// <summary>
        /// The string argument '{argumentName}' cannot be empty.
        /// </summary>
        public static string ArgumentIsEmpty([CanBeNull] string argumentName)
        {
            return string.Format($"The string argument '{argumentName}' cannot be empty.", argumentName);
        }

        /// <summary>
        /// The entity type '{type}' provided for the argument '{argumentName}' must be a reference type.
        /// </summary>
        public static string InvalidEntityType([CanBeNull] Type type, [CanBeNull] string argumentName)
        {
            return string.Format($"The entity type '{type}' provided for the argument '{argumentName}' must be a reference type.", type, argumentName);
        }

        /// <summary>
        /// The collection argument '{argumentName}' must contain at least one element.
        /// </summary>
        public static string CollectionArgumentIsEmpty([CanBeNull] string argumentName)
        {
            return string.Format($"The collection argument '{argumentName}' must contain at least one element.", argumentName);
        }
    }
}