using Microsoft.EntityFrameworkCore;
using Kanbela.Api.Domain;

namespace Kanbela.Api.Data;

public class KanbelaDbContext(DbContextOptions<KanbelaDbContext> options) : DbContext(options)
{
    public DbSet<Board> Boards => Set<Board>();

    public DbSet<BoardColumn> Columns => Set<BoardColumn>();

    public DbSet<Card> Cards => Set<Card>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Board>(entity =>
        {
            entity.HasKey(board => board.Id);
            entity.Property(board => board.Title).IsRequired().HasMaxLength(200);
            entity.HasMany(board => board.Columns)
                .WithOne(column => column.Board)
                .HasForeignKey(column => column.BoardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BoardColumn>(entity =>
        {
            entity.HasKey(column => column.Id);
            entity.Property(column => column.Name).IsRequired().HasMaxLength(120);
            entity.HasMany(column => column.Cards)
                .WithOne(card => card.Column)
                .HasForeignKey(card => card.ColumnId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Card>(entity =>
        {
            entity.HasKey(card => card.Id);
            entity.Property(card => card.Title).IsRequired().HasMaxLength(200);
            entity.Property(card => card.Description).HasMaxLength(2000);
            entity.HasIndex(card => new { card.ColumnId, card.Position });
        });
    }
}
