using System;
using System.Diagnostics;

namespace Attribute
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DontApplyToListElementsAttribute : System.Attribute
    {
    }

    [DontApplyToListElements]
    [AttributeUsage(AttributeTargets.All)]
    [Conditional("UNITY_EDITOR")]
    public class LabelTextAttribute : System.Attribute
    {
        /// <summary>The new text of the label.</summary>
        public string Text;
        /// <summary>
        /// Whether the label text should be nicified before it is displayed, IE, "m_someField" becomes "Some Field".
        /// If the label text is resolved via a member reference, an expression, or the like, then the evaluated result
        /// of that member reference or expression will be nicified.
        /// </summary>
        public bool NicifyText;

        /// <summary>Give a property a custom label.</summary>
        /// <param name="text">The new text of the label.</param>
        public LabelTextAttribute(string text) => Text = text;

        /// <summary>Give a property a custom label.</summary>
        /// <param name="text">The new text of the label.</param>
        /// <param name="nicifyText">Whether to nicify the label text.</param>
        public LabelTextAttribute(string text, bool nicifyText)
        {
            Text = text;
            NicifyText = nicifyText;
        }
    }
}