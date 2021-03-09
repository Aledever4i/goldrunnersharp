using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using SwaggerDateConverter = goldrunnersharp.Client.SwaggerDateConverter;

namespace goldrunnersharp.Model
{
    /// <summary>
    /// License for digging.
    /// </summary>
    [DataContract]
    public partial class License :  IEquatable<License>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="License" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected License() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="License" /> class.
        /// </summary>
        /// <param name="id">id (required).</param>
        /// <param name="digAllowed">digAllowed (required).</param>
        /// <param name="digUsed">digUsed (required).</param>
        public License(int? id = default(int?), int digAllowed = default(int), int digUsed = default)
        {
            this.Id = id;
            this.DigAllowed = digAllowed;
            this.DigUsed = digUsed;
        }
        
        /// <summary>
        /// Gets or Sets Id
        /// </summary>
        [DataMember(Name="id", EmitDefaultValue=false)]
        public int? Id { get; set; }

        /// <summary>
        /// Gets or Sets DigAllowed
        /// </summary>
        [DataMember(Name="digAllowed", EmitDefaultValue=false)]
        public int DigAllowed { get; set; }

        /// <summary>
        /// Gets or Sets DigUsed
        /// </summary>
        [DataMember(Name="digUsed", EmitDefaultValue=false)]
        public int DigUsed { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class License {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  DigAllowed: ").Append(DigAllowed).Append("\n");
            sb.Append("  DigUsed: ").Append(DigUsed).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }
  
        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return this.Equals(input as License);
        }

        /// <summary>
        /// Returns true if License instances are equal
        /// </summary>
        /// <param name="input">Instance of License to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(License input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Id == input.Id ||
                    (this.Id != null &&
                    this.Id.Equals(input.Id))
                ) && 
                (
                    this.DigAllowed == input.DigAllowed || this.DigAllowed.Equals(input.DigAllowed)
                ) && 
                (
                    this.DigUsed == input.DigUsed || this.DigUsed.Equals(input.DigUsed)
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (this.Id != null)
                    hashCode = hashCode * 59 + this.Id.GetHashCode();
                if (this.DigAllowed != null)
                    hashCode = hashCode * 59 + this.DigAllowed.GetHashCode();
                if (this.DigUsed != null)
                    hashCode = hashCode * 59 + this.DigUsed.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }

}
