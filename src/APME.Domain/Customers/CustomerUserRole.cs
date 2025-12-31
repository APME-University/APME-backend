using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Identity;

namespace APME.Customers;

public class CustomerUserRole : Entity<Guid>
{
    /// <summary>
    /// Gets or sets the primary key of the Customer user that is linked to a role.
    /// </summary>
    public virtual Guid UserId { get; protected set; }
    [ForeignKey(nameof(UserId))]
    public virtual Customer Customer { get; set; }
    /// <summary>
    /// Gets or sets the primary key of the role that is linked to the user.
    /// </summary>
    public virtual Guid RoleId { get; protected set; }
    [ForeignKey(nameof(RoleId))]
    public virtual IdentityRole Role { get; set; }

    protected CustomerUserRole()
    {
    }

    protected internal CustomerUserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    public override object[] GetKeys()
    {
        return new object[] { UserId, RoleId };
    }
}
