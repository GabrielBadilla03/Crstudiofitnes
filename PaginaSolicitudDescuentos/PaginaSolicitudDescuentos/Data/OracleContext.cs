using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PaginaSolicitudDescuentos.Models;

namespace PaginaSolicitudDescuentos.Data;

public partial class OracleContext : DbContext
{
    public OracleContext(DbContextOptions<OracleContext> options)
        : base(options)
    {
    }

    public virtual DbSet<XXORA_CUSTOMER_MASTER> XXORA_CUSTOMER_MASTERs { get; set; }

    public virtual DbSet<XXORA_DISCOUNT_LIST> XXORA_DISCOUNT_LISTs { get; set; }

    public virtual DbSet<XXORA_ITEM_MASTER> XXORA_ITEM_MASTERs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder
            .HasDefaultSchema("BG_INTUSER")
            .UseCollation("USING_NLS_COMP");

        modelBuilder.Entity<XXORA_CUSTOMER_MASTER>(entity =>
        {
            entity
                .ToTable("XXORA_CUSTOMER_MASTER");

            entity.HasKey(e => e.REGISTRY_ID);

            entity.HasIndex(e => e.ACCOUNT_ID, "IDX_CUSTOMER_ACCOUNT_ID");

            entity.HasIndex(e => e.BU_NOMBRE, "IDX_CUSTOMER_BU");

            entity.HasIndex(e => e.IDCLIENTE, "IDX_CUSTOMER_IDCLIENTE");

            entity.HasIndex(e => e.ORGANIZATION_ID, "IDX_CUSTOMER_ORGANIZATION_ID");

            entity.HasIndex(e => e.PARTY_ID, "IDX_CUSTOMER_PARTY_ID");

            entity.HasIndex(e => e.PARTY_SITE_NUMBER, "IDX_CUSTOMER_PARTY_SITE");

            entity.HasIndex(e => e.VENDEDOR, "IDX_CUSTOMER_SALESPEERSON");

            entity.HasIndex(e => e.SITIO, "IDX_CUSTOMER_SITE");

            entity.HasIndex(e => new { e.SITIO, e.SITIO_ESTATUS, e.RUTA }, "IDX_CUSTOMER_SITE_ESTADO_RUTA");

            entity.Property(e => e.ACCOUNT_ID).HasColumnType("NUMBER");
            entity.Property(e => e.ACCT_LAST_UPDATE_DATE).HasColumnType("DATE");
            entity.Property(e => e.AR_NUMERO)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.BILL_TO_SITE)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.BILL_TO_SITE_USE_ID).HasColumnType("NUMBER");
            entity.Property(e => e.BU_NOMBRE)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CATEGORIA)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CEDULA)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CLIENTE_ESTATUS)
                .HasMaxLength(1)
                .IsUnicode(false);
            entity.Property(e => e.CUST_ACCT_SITE_ID).HasColumnType("NUMBER");
            entity.Property(e => e.EMAIL_CLIENTE)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.GRUPO_CLIENTE)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.IDCLIENTE)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.IDVENDEDOR)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.LATITUD_MUNICIPIO).HasColumnType("NUMBER");
            entity.Property(e => e.LIMITECREDITO).HasColumnType("NUMBER");
            entity.Property(e => e.LIMITECREDITO_MONEDA)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.LONGITUD_MUNICIPIO).HasColumnType("NUMBER");
            entity.Property(e => e.NOMBRE_CLASECLIENTE)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.NOMBRE_CLIENTE)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NOMBRE_SITIO)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.ORGANIZATION_ID).HasColumnType("NUMBER");
            entity.Property(e => e.PAIS)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.PARTY_ID).HasColumnType("NUMBER");
            entity.Property(e => e.PARTY_NAME)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.PARTY_SITE_ID).HasColumnType("NUMBER");
            entity.Property(e => e.PARTY_SITE_NUMBER)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.PARTY_SITE_PRIMARY_FLAG)
                .HasMaxLength(1)
                .IsUnicode(false);
            entity.Property(e => e.REGISTRY_ID)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.RUTA)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.SITE_LAST_UPDATE_DATE).HasColumnType("DATE");
            entity.Property(e => e.SITE_USE_ID).HasColumnType("NUMBER");
            entity.Property(e => e.SITIO)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.SITIO_CANTON)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.SITIO_CIUDAD)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.SITIO_DIR1)
                .HasMaxLength(240)
                .IsUnicode(false);
            entity.Property(e => e.SITIO_DIR2)
                .HasMaxLength(240)
                .IsUnicode(false);
            entity.Property(e => e.SITIO_DIR3)
                .HasMaxLength(240)
                .IsUnicode(false);
            entity.Property(e => e.SITIO_DISTRITO)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.SITIO_ESTADO)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.SITIO_ESTATUS)
                .HasMaxLength(1)
                .IsUnicode(false);
            entity.Property(e => e.SITIO_POSTALCODE)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SITIO_PROVINCIA)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.TELEFONO1_CLIENTE)
                .HasMaxLength(16)
                .IsUnicode(false);
            entity.Property(e => e.TERMINO_PAGO)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.VENDEDOR)
                .HasMaxLength(150)
                .IsUnicode(false);
        });

        modelBuilder.Entity<XXORA_DISCOUNT_LIST>(entity =>
        {
            entity
                .ToTable("XXORA_DISCOUNT_LIST");

            entity.HasKey(e => e.ITEM_NUMBER);

            entity.Property(e => e.BU_NAME)
                .HasMaxLength(240)
                .IsUnicode(false);
            entity.Property(e => e.CREATED_BY)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.CREATION_DATE).HasPrecision(6);
            entity.Property(e => e.CURRENCY_CODE)
                .HasMaxLength(12)
                .IsUnicode(false);
            entity.Property(e => e.DISCOUNT_LIST_ID).HasPrecision(18);
            entity.Property(e => e.DISCOUNT_LIST_ITEM_ID).HasPrecision(18);
            entity.Property(e => e.DISCOUNT_LIST_NAME)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.DISCOUNT_PRICE).HasColumnType("NUMBER");
            entity.Property(e => e.DISCOUNT_TYPE)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.END_DATE).HasPrecision(6);
            entity.Property(e => e.ITEM_NUMBER)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.LAST_UPDATED_BY)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.LAST_UPDATE_DATE).HasPrecision(6);
            entity.Property(e => e.PARTY_NUMBER)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.PRICING_RULE_TYPE_CODE)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.PRICING_UOM_CODE)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.RULE_DISCOUNT_NAME)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.START_DATE).HasPrecision(6);
            entity.Property(e => e.STATUS)
                .HasMaxLength(3)
                .IsUnicode(false);
        });

        modelBuilder.Entity<XXORA_ITEM_MASTER>(entity =>
        {
            entity
                .ToTable("XXORA_ITEM_MASTER");

            entity.HasKey(e => e.ITEM_NUMBER);

            entity.Property(e => e.BU_NAME)
                .HasMaxLength(240)
                .IsUnicode(false);
            entity.Property(e => e.CASE_PACK_QUANTITY).HasColumnType("NUMBER");
            entity.Property(e => e.CATEGORY_CODE)
                .HasMaxLength(820)
                .IsUnicode(false);
            entity.Property(e => e.CATEGORY_NAME)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.CREATED_BY)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.CREATION_DATE).HasPrecision(6);
            entity.Property(e => e.DESCRIPTION)
                .HasMaxLength(240)
                .IsUnicode(false);
            entity.Property(e => e.ITEM_NUMBER)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.LAST_UPDATED_BY)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.LAST_UPDATE_DATE).HasPrecision(6);
            entity.Property(e => e.LONG_DESCRIPTION)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.ORGANIZATION_CODE)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.ORIGIN_COUNTRY)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.PRIMARY_UOM_CODE)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.SECONDARY_UOM_CODE)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.STATUS)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.SUBCATEGORY_CODE)
                .HasMaxLength(820)
                .IsUnicode(false);
            entity.Property(e => e.SUBCATEGORY_NAME)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.TAX_CLASSIFICATION_CODE)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TAX_RATE).HasColumnType("NUMBER");
            entity.Property(e => e.UNIT_WEIGHT).HasColumnType("NUMBER");
            entity.Property(e => e.WEIGHT_UOM_CODE)
                .HasMaxLength(3)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
