﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using API_PCC.EntityModels;
using System;
using System.Collections;
using System.Collections.Generic;
using static AngouriMath.MathS;

namespace API_PCC.Models;

public partial class HBuffHerd
{
    public int Id { get; set; }

    public string HerdName { get; set; }

    public string HerdCode { get; set; }

    public int HerdSize { get; set; }
    public string FarmAffilCode { get; set; }

    public string HerdClassDesc { get; set; }

    public ICollection<HBuffaloType>? buffaloType { get; set; } = new List<HBuffaloType>();


    public ICollection<HFeedingSystem>? feedingSystem { get; set; } = new List<HFeedingSystem>();

    public string FarmManager { get; set; }

    public string FarmAddress { get; set; }

    public int Owner { get; set; }

    public int Status { get; set; }

    public DateTime DateCreated { get; set; }

    public DateTime? DateUpdated { get; set; }

    public bool DeleteFlag { get; set; }

    public string CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DateDeleted { get; set; }

    public string? DeletedBy { get; set; }

    public DateTime? DateRestored { get; set; }

    public string? RestoredBy { get; set; }
    public string? OrganizationName { get; set; }
    public string? Center { get; set; }
    public string? Photo { get; set; }
}