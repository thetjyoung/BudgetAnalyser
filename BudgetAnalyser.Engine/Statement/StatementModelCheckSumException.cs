using System;
using System.IO;
using System.Runtime.Serialization;

namespace BudgetAnalyser.Engine.Statement
{
    /// <summary>
    /// An exception to represent an inconsistency in the <see cref="StatementModel"/> loaded. The check sum does not match the data.
    /// </summary>
    [Serializable]
    public class StatementModelCheckSumException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatementModelCheckSumException"/> class.        
        /// </summary>
        /// <param name="checksum">The actual checksum of the file.</param>
        public StatementModelCheckSumException(string checksum)
            : base()
        {
            this.FileChecksum = checksum;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatementModelCheckSumException"/> class.        
        /// </summary>
        /// <param name="checksum">The actual checksum of the file.</param>
        /// <param name="message">The message.</param>
        public StatementModelCheckSumException(string checksum, string message)
            : base(message)
        {
            this.FileChecksum = checksum;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatementModelCheckSumException"/> class.        
        /// </summary>
        /// <param name="checksum">The actual checksum of the file.</param>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public StatementModelCheckSumException(string checksum, string message, Exception innerException)
            : base(message, innerException)
        {
            this.FileChecksum = checksum;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatementModelCheckSumException"/> class.        
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="info"/> parameter is null. </exception>
        /// <exception cref="T:System.Runtime.Serialization.SerializationException">The class name is null or <see cref="P:System.Exception.HResult"/> is zero (0). </exception>
        protected StatementModelCheckSumException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public string FileChecksum { get; private set; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("FileChecksum", FileChecksum);
        }
    }
}
