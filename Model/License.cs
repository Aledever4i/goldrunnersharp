using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace goldrunnersharp.Model
{
    [DataContract]
    public partial class License :  IEquatable<License>, IValidatableObject//, INotifyPropertyChanged
    {
        [JsonConstructor]
        protected License() { }

        //private int _digUsed { get; set; }

        public License(int? id = null, int digAllowed = 0, int digUsed = 0)
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
        public int DigUsed {
            get;
            set;
        }


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
  
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public override bool Equals(object input)
        {
            return this.Equals(input as License);
        }

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

        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }

}
