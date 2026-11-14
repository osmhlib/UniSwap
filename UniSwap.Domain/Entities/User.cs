using System;
using System.Collections.Generic;
using System.Text;
using UniSwap.Domain.ValueObjects;

namespace UniSwap.Domain.Entities
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public Email Email { get; private set; }

        public User(string name, Email email)
        {
            Id = Guid.NewGuid();
            Name = name;
            Email = email;
        }
    }
}
