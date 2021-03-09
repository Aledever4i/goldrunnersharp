using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace goldrunnersharp.Model
{
    /// <summary>
    /// Area
    /// </summary>
    [DataContract]
    public partial class Area :  IEquatable<Area>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Area" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected Area() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Area" /> class.
        /// </summary>
        /// <param name="posX">posX (required).</param>
        /// <param name="posY">posY (required).</param>
        /// <param name="sizeX">sizeX.</param>
        /// <param name="sizeY">sizeY.</param>
        public Area(int? posX = default(int?), int? posY = default(int?), int? sizeX = default(int?), int? sizeY = default(int?))
        {
            this.PosX = posX;
            this.PosY = posY;
            this.SizeX = sizeX;
            this.SizeY = sizeY;
        }
        
        /// <summary>
        /// Gets or Sets PosX
        /// </summary>
        [DataMember(Name="posX", EmitDefaultValue=false)]
        public int? PosX { get; set; }

        /// <summary>
        /// Gets or Sets PosY
        /// </summary>
        [DataMember(Name="posY", EmitDefaultValue=false)]
        public int? PosY { get; set; }

        /// <summary>
        /// Gets or Sets SizeX
        /// </summary>
        [DataMember(Name="sizeX", EmitDefaultValue=false)]
        public int? SizeX { get; set; }

        /// <summary>
        /// Gets or Sets SizeY
        /// </summary>
        [DataMember(Name="sizeY", EmitDefaultValue=false)]
        public int? SizeY { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class Area {\n");
            sb.Append("  PosX: ").Append(PosX).Append("\n");
            sb.Append("  PosY: ").Append(PosY).Append("\n");
            sb.Append("  SizeX: ").Append(SizeX).Append("\n");
            sb.Append("  SizeY: ").Append(SizeY).Append("\n");
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
            return this.Equals(input as Area);
        }

        /// <summary>
        /// Returns true if Area instances are equal
        /// </summary>
        /// <param name="input">Instance of Area to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(Area input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.PosX == input.PosX ||
                    (this.PosX != null &&
                    this.PosX.Equals(input.PosX))
                ) && 
                (
                    this.PosY == input.PosY ||
                    (this.PosY != null &&
                    this.PosY.Equals(input.PosY))
                ) && 
                (
                    this.SizeX == input.SizeX ||
                    (this.SizeX != null &&
                    this.SizeX.Equals(input.SizeX))
                ) && 
                (
                    this.SizeY == input.SizeY ||
                    (this.SizeY != null &&
                    this.SizeY.Equals(input.SizeY))
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
                if (this.PosX != null)
                    hashCode = hashCode * 59 + this.PosX.GetHashCode();
                if (this.PosY != null)
                    hashCode = hashCode * 59 + this.PosY.GetHashCode();
                if (this.SizeX != null)
                    hashCode = hashCode * 59 + this.SizeX.GetHashCode();
                if (this.SizeY != null)
                    hashCode = hashCode * 59 + this.SizeY.GetHashCode();
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
            // PosX (int?) minimum
            if(this.PosX < (int?)0)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for PosX, must be a value greater than or equal to 0.", new [] { "PosX" });
            }

            // PosY (int?) minimum
            if(this.PosY < (int?)0)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for PosY, must be a value greater than or equal to 0.", new [] { "PosY" });
            }

            // SizeX (int?) minimum
            if(this.SizeX < (int?)1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for SizeX, must be a value greater than or equal to 1.", new [] { "SizeX" });
            }

            // SizeY (int?) minimum
            if(this.SizeY < (int?)1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for SizeY, must be a value greater than or equal to 1.", new [] { "SizeY" });
            }

            yield break;
        }
    }

}
