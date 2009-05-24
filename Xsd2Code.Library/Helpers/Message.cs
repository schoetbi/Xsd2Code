#region Namespace references

using System;
using System.Threading;

#endregion

namespace Xsd2Code.Library.Helpers
{
    /// <summary>
    /// Message class represents a message generated at a point of execution
    /// </summary>
    /// <remarks>
    /// Revision history:
    /// 
    ///     Created 2009-02-20 by Ruslan Urban
    /// 
    /// </remarks>
    public class Message
    {
        private string _ruleName = string.Empty;

        /// <summary>
        /// Message default constructor
        /// </summary>
        public Message()
        {
            this.MessageSubtype = MessageSubtype.Unspecified;
        }

        /// <summary>
        /// Message constructor with initializers
        /// </summary>
        /// <param name="messageType">Message type</param>
        /// <param name="text">Message text</param>
        public Message(MessageType messageType, string text)
            : this(messageType, string.Empty, text)
        {}

        /// <summary>
        /// Message constructor with initializers
        /// </summary>
        /// <param name="messageType">Message type</param>
        /// <param name="ruleName">Name of business rule</param>
        /// <param name="text">Message text</param>
        public Message(MessageType messageType, string ruleName, string text) : this()
        {
            this.MessageType = messageType;
            this._ruleName = ruleName;
            this.Text = text ?? string.Empty;
        }

        /// <summary>
        /// Message constructor with initializers
        /// </summary>
        /// <param name="messageType">Message type</param>
        /// <param name="ruleName">Name of business rule</param>
        /// <param name="format">Message text format string</param>
        /// <param name="args">Format string arguments</param>
        public Message(MessageType messageType, string ruleName, string format, params object[] args)
            : this(messageType, ruleName, string.Format(Thread.CurrentThread.CurrentCulture, format, args))
        {}

        /// <summary>
        /// Message constructor with initializers
        /// </summary>
        /// <param name="messageType">Message type</param>
        /// <param name="format">Message text</param>
        /// <param name="args">Arguments</param>
        public Message(MessageType messageType, string format, params object[] args)
            : this(messageType, string.Empty, string.Format(Thread.CurrentThread.CurrentCulture, format, args))
        {}


        /// <summary>
        /// Message class constructor
        /// </summary>
        /// <param name="text">parameter value</param>
        public Message(string text) : this(default(MessageType), text)
        {}


        /// <summary>
        /// Message class constructor
        /// </summary>
        /// <param name="format">Format</param>
        /// <param name="args">Arguments</param>
        public Message(string format, params object[] args)
            : this(default(MessageType), string.Format(Thread.CurrentThread.CurrentCulture, format, args))
        {}


        /// <summary>
        /// Rule Name
        /// </summary>
        public string RuleName
        {
            get { return this._ruleName; }
            set { this._ruleName = value; }
        }


        /// <summary>
        /// Message Type
        /// </summary>
        public MessageType MessageType { get; set; }


        /// <summary>
        /// Message Subtype
        /// </summary>
        public MessageSubtype MessageSubtype { get; set; }


        /// <summary>
        /// Text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Represents message object in a form of a string
        /// </summary>
        /// <returns>Formatted Message</returns>
        public override string ToString()
        {
            return string.Format(Thread.CurrentThread.CurrentCulture, "{1}: {2}{0}\tSubType: {3}{0}{0}\tRule: {4}",
                                 Environment.NewLine, this.MessageType, this.Text, this.MessageSubtype,
                                 this.RuleName);
        }
    }
}