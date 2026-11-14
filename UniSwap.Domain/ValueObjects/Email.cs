using System;
using System.Collections.Generic;
using System.Text;

namespace UniSwap.Domain.ValueObjects
{
    public class Email
    {
        public string Value { get; }

        public Email(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email cannot be empty.");

            if (!value.Contains("@"))
                throw new ArgumentException("Invalid email format.");

            Value = value;
        }

        public override string ToString()
        {
            return Value;
        }

        public override bool Equals(object? obj)
        {
            return obj is Email email && Value == email.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
