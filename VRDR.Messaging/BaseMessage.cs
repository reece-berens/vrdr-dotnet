using System;
using System.Linq;
using System.Collections.Generic;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace VRDR
{
    /// <summary>Class <c>BaseMessage</c> is the base class of all messages.</summary>
    public class BaseMessage
    {
        /// <summary>Useful for navigating around the FHIR Bundle using FHIRPaths.</summary>
        protected ITypedElement Navigator;

        /// <summary>Bundle that contains the message.</summary>
        protected Bundle MessageBundle;

        /// <summary>MessageHeader that contains the message header.</summary>
        protected MessageHeader Header;

        /// <summary>Constructor that creates a new, empty message for the specified message type.</summary>
        public BaseMessage(String messageType)
        {
            // Start with a Bundle.
            MessageBundle = new Bundle();
            MessageBundle.Id = Guid.NewGuid().ToString();
            MessageBundle.Type = Bundle.BundleType.Message;

            // Start with a MessageHeader.
            Header = new MessageHeader();
            Header.Id = Guid.NewGuid().ToString();
            Header.Timestamp = DateTime.Now;

            // No URI in STU3 so use Coding instead.
            //Header.Event.Add(new Uri("http://nchs.cdc.gov/" + messageType"));
            Header.Event = new Coding("http://nchs.cdc.gov/", messageType, messageType);

            MessageHeader.MessageDestinationComponent dest = new MessageHeader.MessageDestinationComponent();
            dest.Endpoint = "http://nchs.cdc.gov/vrdr_submission";
            Header.Destination.Add(dest);
            MessageHeader.MessageSourceComponent src = new MessageHeader.MessageSourceComponent();
            src.Endpoint = "nightingale";
            Header.Source = src;

            MessageBundle.AddResourceEntry(Header, "urn:uuid:" + Header.Id);

            // Create a Navigator for this.
            Navigator = MessageBundle.ToTypedElement();
        }

        /// <summary>Constructor that takes a string that represents a DeathRecordSubmission message in either XML or JSON format.</summary>
        /// <param name="message">represents a DeathRecordSubmission message in either XML or JSON format.</param>
        /// <param name="permissive">if the parser should be permissive when parsing the given string</param>
        /// <exception cref="ArgumentException">Message is neither valid XML nor JSON.</exception>
        public BaseMessage(string message, bool permissive = false)
        {
            ParserSettings parserSettings = new ParserSettings { AcceptUnknownMembers = permissive,
                                                                 AllowUnrecognizedEnums = permissive,
                                                                 PermissiveParsing = permissive };
            // XML?
            if (!String.IsNullOrEmpty(message) && message.TrimStart().StartsWith("<"))
            {
                // Grab all errors found by visiting all nodes and report if not permissive
                if (!permissive)
                {
                    List<string> entries = new List<string>();
                    ISourceNode node = FhirXmlNode.Parse(message, new FhirXmlParsingSettings { PermissiveParsing = permissive });
                    foreach (Hl7.Fhir.Utility.ExceptionNotification problem in node.VisitAndCatch())
                    {
                        entries.Add(problem.Message);
                    }
                    if (entries.Count > 0)
                    {
                        throw new System.ArgumentException(String.Join("; ", entries).TrimEnd());
                    }
                }
                // Try Parse
                try
                {
                    FhirXmlParser parser = new FhirXmlParser(parserSettings);
                    MessageBundle = parser.Parse<Bundle>(message);
                    Navigator = MessageBundle.ToTypedElement();
                }
                catch (Exception e)
                {
                    throw new System.ArgumentException(e.Message);
                }
            }
            // JSON?
            if (!String.IsNullOrEmpty(message) && message.TrimStart().StartsWith("{"))
            {
                // Grab all errors found by visiting all nodes and report if not permissive
                if (!permissive)
                {
                    List<string> entries = new List<string>();
                    ISourceNode node = FhirJsonNode.Parse(message, "Bundle", new FhirJsonParsingSettings { PermissiveParsing = permissive });
                    foreach (Hl7.Fhir.Utility.ExceptionNotification problem in node.VisitAndCatch())
                    {
                        entries.Add(problem.Message);
                    }
                    if (entries.Count > 0)
                    {
                        throw new System.ArgumentException(String.Join("; ", entries).TrimEnd());
                    }
                }
                // Try Parse
                try
                {
                    FhirJsonParser parser = new FhirJsonParser(parserSettings);
                    MessageBundle = parser.Parse<Bundle>(message);
                    Navigator = MessageBundle.ToTypedElement();
                }
                catch (Exception e)
                {
                    throw new System.ArgumentException(e.Message);
                }
            }
            // Fill out class instance references
            if (Navigator != null)
            {
                RestoreReferences();
            }
            else
            {
                throw new System.ArgumentException("The given input does not appear to be a valid XML or JSON FHIR message.");
            }
        }

        /// <summary>Helper method to return a XML string representation of this DeathRecordSubmission.</summary>
        /// <returns>a string representation of this DeathRecordSubmission in XML format</returns>
        public string ToXML()
        {
            return MessageBundle.ToXml();
        }

        /// <summary>Helper method to return a XML string representation of this DeathRecordSubmission.</summary>
        /// <returns>a string representation of this DeathRecordSubmission in XML format</returns>
        public string ToXml()
        {
            return MessageBundle.ToXml();
        }

        /// <summary>Helper method to return a JSON string representation of this DeathRecordSubmission.</summary>
        /// <returns>a string representation of this DeathRecordSubmission in JSON format</returns>
        public string ToJSON()
        {
            return MessageBundle.ToJson();
        }

        /// <summary>Helper method to return a JSON string representation of this DeathRecordSubmission.</summary>
        /// <returns>a string representation of this DeathRecordSubmission in JSON format</returns>
        public string ToJson()
        {
            return MessageBundle.ToJson();
        }

        /// <summary>Helper method to return an ITypedElement of the message bundle.</summary>
        /// <returns>an ITypedElement of the message bundle</returns>
        public ITypedElement GetITypedElement()
        {
            return Navigator;
        }


        /////////////////////////////////////////////////////////////////////////////////
        //
        // Message Properties
        //
        /////////////////////////////////////////////////////////////////////////////////

        /// <summary>Message timestamp</summary>
        /// <value>the message timestamp.</value>
        public DateTimeOffset? MessageTimestamp
        {
            get
            {
                return Header.Timestamp;
            }
            set
            {
                Header.Timestamp = value;
            }
        }

        /// <summary>Message Id</summary>
        /// <value>the message id.</value>
        public string MessageId
        {
            get
            {
                return Header.Id;
            }
            set
            {
                Header.Id = value;
                MessageBundle.Entry.RemoveAll( entry => entry.Resource.ResourceType == ResourceType.MessageHeader );
                MessageBundle.AddResourceEntry(Header, "urn:uuid:" + Header.Id);
            }
        }

        /// <summary>Message Type</summary>
        /// <value>the message type.</value>
        public string MessageType
        {
            get
            {
                return Header.Event.Code;
            }
            set
            {
                Header.Event = new Coding("http://nchs.cdc.gov/", value, value);
            }
        }

        /// <summary>Message Source</summary>
        /// <value>the message source.</value>
        public string MessageSource
        {
            get
            {
                return Header.Source.Endpoint;
            }
            set
            {
                Header.Source.Endpoint = value;
            }
        }

        /// <summary>Message Destination</summary>
        /// <value>the message destination.</value>
        public string MessageDestination
        {
            get
            {
                return Header.Destination.ToArray()[0].Endpoint;
            }
            set
            {
                Header.Destination.Clear();
                MessageHeader.MessageDestinationComponent dest = new MessageHeader.MessageDestinationComponent();
                dest.Endpoint = value;
                Header.Destination.Add(dest);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        // Class helper methods useful for building, searching through messages.
        //
        /////////////////////////////////////////////////////////////////////////////////

        /// <summary>Restores class references from a newly parsed record.</summary>
        protected virtual void RestoreReferences()
        {
            // Grab Header
            var headerEntry = MessageBundle.Entry.FirstOrDefault( entry => entry.Resource.ResourceType == ResourceType.MessageHeader );
            if (headerEntry != null)
            {
                Header = (MessageHeader)headerEntry.Resource;
            }
            else
            {
                throw new System.ArgumentException("Failed to find a Header. The first entry in the FHIR Message should be a MessageHeader.");
            }
        }
    }
}