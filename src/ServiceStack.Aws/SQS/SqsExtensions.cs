using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Amazon.SQS;
using Amazon.SQS.Model;
using ServiceStack.Aws.Support;
using ServiceStack.Messaging;
using ServiceStack.Text;
using Message = Amazon.SQS.Model.Message;

namespace ServiceStack.Aws.SQS
{
    public static class SqsExtensions
    {
        public static Exception ToException(this BatchResultErrorEntry entry, [CallerMemberName] string methodOperation = null)
        {
            return entry == null
                       ? null
                       : new Exception("Batch Entry exception for operation [{0}]. Id [{1}], Code [{2}], Is Sender Fault [{3}].".Fmt(methodOperation,
                                                                                                                                     entry.Id,
                                                                                                                                     entry.Code,
                                                                                                                                     entry.SenderFault));
        }

        public static string ToValidQueueName(this string queueName)
        {
            if (IsValidQueueName(queueName))
            {
                return queueName;
            }

            var validQueueName = Regex.Replace(queueName, @"([^\d\w-_])", "-");
            return validQueueName;
        }

        public static bool IsValidQueueName(this string queueName)
        {
            return queueName.All(c => char.IsLetterOrDigit(c) ||
                                      SqsQueueDefinition.ValidNonAlphaNumericChars.Contains(c));
        }

        public static SetQueueAttributesRequest ToSetAttributesRequest(this CreateQueueRequest request, string queueUrl)
        {
            return new SetQueueAttributesRequest
                   {
                       QueueUrl = queueUrl,
                       Attributes = request.Attributes
                   };
        }
        
        public static SqsQueueDefinition ToQueueDefinition(this Dictionary<string, string> attributes, SqsQueueName queueName,
                                                           string queueUrl, bool disableBuffering)
        {
            var attrToUse = attributes ?? new Dictionary<string, string>();

            Guard.AgainstNullArgument(queueName, "queueName");
            Guard.AgainstNullArgument(queueUrl, "queueUrl");

            return new SqsQueueDefinition
                   {
                       SqsQueueName = queueName,
                       QueueUrl = queueUrl,
                       VisibilityTimeout = attrToUse.ContainsKey(QueueAttributeName.VisibilityTimeout)
                                               ? attrToUse[QueueAttributeName.VisibilityTimeout].ToInt(SqsQueueDefinition.DefaultVisibilityTimeoutSeconds)
                                               : SqsQueueDefinition.DefaultVisibilityTimeoutSeconds,
                       ReceiveWaitTime = attrToUse.ContainsKey(QueueAttributeName.ReceiveMessageWaitTimeSeconds)
                                             ? attrToUse[QueueAttributeName.ReceiveMessageWaitTimeSeconds].ToInt(SqsQueueDefinition.DefaultWaitTimeSeconds)
                                             : SqsQueueDefinition.DefaultWaitTimeSeconds,
                       CreatedTimestamp = attrToUse.ContainsKey(QueueAttributeName.CreatedTimestamp)
                                              ? attrToUse[QueueAttributeName.CreatedTimestamp].ToInt64(DateTime.UtcNow.ToUnixTime())
                                              : DateTime.UtcNow.ToUnixTime(),
                       DisableBuffering = disableBuffering,
                       ApproximateNumberOfMessages = attrToUse.ContainsKey(QueueAttributeName.ApproximateNumberOfMessages)
                                                         ? attrToUse[QueueAttributeName.ApproximateNumberOfMessages].ToInt64(0)
                                                         : 0,
                       QueueArn = attrToUse.ContainsKey(QueueAttributeName.QueueArn)
                                      ? attrToUse[QueueAttributeName.QueueArn]
                                      : null,
                       RedrivePolicy = attrToUse.ContainsKey(QueueAttributeName.RedrivePolicy)
                                           ? attrToUse[QueueAttributeName.RedrivePolicy].FromJson<SqsRedrivePolicy>()
                                           : null
                   };
        }

        public static Message<T> ToMessage<T>(this Message sqsMessage, String queueName)
        {
            if (sqsMessage == null)
            {
                return null;
            }

            Guard.AgainstNullArgument(queueName, "queueName");

            var message = sqsMessage.Body.FromJsv<Message<T>>();

            message.Tag = SqsMessageTag.CreateTag(queueName, sqsMessage.ReceiptHandle);

            return message;
        }

        public static String ToSha256HashString64(this String toHash, Encoding encoding = null)
        {
            if (String.IsNullOrEmpty(toHash))
            {
                return String.Empty;
            }
            if (encoding == null)
            {
                encoding = Encoding.Unicode;
            }

            var bytes = encoding.GetBytes(toHash).ToSha256ByteHash();
            return Convert.ToBase64String(bytes);
        }

        public static Byte[] ToSha256ByteHash(this Byte[] byteBuffer)
        {
            if (byteBuffer == null || !byteBuffer.Any())
            {
                return null;
            }

            var ha = SHA256.Create();

            if (ha == null)
            {
                return null;
            }

            var hashValue = ha.ComputeHash(byteBuffer);
            ha.Clear();
            return hashValue;
        }


    }
}