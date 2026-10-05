using InternetCafe.API.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternetCafe.API.Data;

public sealed class InternetCafeDbContext(DbContextOptions<InternetCafeDbContext> options)
    : DbContext(options)
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Computer> Computers => Set<Computer>();
    public DbSet<MembershipTier> MembershipTiers => Set<MembershipTier>();
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<WorkShift> WorkShifts => Set<WorkShift>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Combo> Combos => Set<Combo>();
    public DbSet<SurveyQuestion> SurveyQuestions => Set<SurveyQuestion>();
    public DbSet<UsageSession> UsageSessions => Set<UsageSession>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();
    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
    public DbSet<FinancialTransaction> Transactions => Set<FinancialTransaction>();
    public DbSet<TopUpReceipt> TopUpReceipts => Set<TopUpReceipt>();
    public DbSet<Feedback> Feedback => Set<Feedback>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<Payroll> Payrolls => Set<Payroll>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<Attendance> Attendances => Set<Attendance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>().HasIndex(x => x.Username).IsUnique();
        modelBuilder.Entity<Customer>().HasIndex(x => x.Username).IsUnique();
        modelBuilder.Entity<Computer>().HasIndex(x => x.Computer_Code).IsUnique();
        modelBuilder.Entity<WorkSchedule>().HasIndex(x => new { x.Employee_ID, x.Work_Date, x.Shift_ID }).IsUnique();
        modelBuilder.Entity<Attendance>().HasIndex(x => x.Schedule_ID).IsUnique();
        modelBuilder.Entity<Payroll>().HasIndex(x => new { x.Employee_ID, x.Pay_Month, x.Pay_Year }).IsUnique();
        modelBuilder.Entity<FinancialTransaction>().HasIndex(x => x.Order_ID)
            .IsUnique().HasFilter("[Order_ID] IS NOT NULL");
        modelBuilder.Entity<FinancialTransaction>().HasIndex(x => x.Session_ID)
            .IsUnique().HasFilter("[Session_ID] IS NOT NULL");
        modelBuilder.Entity<FinancialTransaction>().HasIndex(x => x.Customer_ID);
        modelBuilder.Entity<TopUpReceipt>().HasIndex(x => x.Transaction_ID).IsUnique();

        modelBuilder.Entity<FinancialTransaction>()
            .ToTable("Transactions", "dbo", table =>
            {
                table.HasTrigger("TR_Sync_Customer_Balance");
                table.HasTrigger("TR_Create_TopUp_Receipt");
            });
        modelBuilder.Entity<UsageSession>()
            .ToTable("Usage_Sessions", "dbo", table =>
            {
                table.HasTrigger("TR_Sync_Computer_Status");
                table.HasTrigger("TR_Snapshot_Session_Hourly_Rate");
            });
        modelBuilder.Entity<OrderDetail>()
            .ToTable("Order_Details", "dbo", table =>
            {
                table.HasTrigger("TR_Sync_Order_Details_Inventory");
            });
        modelBuilder.Entity<FinancialTransaction>().Property(x => x.Trans_Date)
            .HasDefaultValueSql("(GETDATE())");

        modelBuilder.Entity<Combo>().Property(x => x.Total_Value)
            .HasComputedColumnSql("([Price]+[Bonus_Balance])", stored: true);
        modelBuilder.Entity<UsageSession>().Property(x => x.Total_Hours)
            .HasComputedColumnSql("(CASE WHEN [End_Time] IS NOT NULL THEN CONVERT(decimal(10,2),DATEDIFF(minute,[Start_Time],[End_Time]))/(60.0) ELSE NULL END)", stored: true);
        modelBuilder.Entity<OrderDetail>().Property(x => x.Line_Total)
            .HasComputedColumnSql("([Quantity]*[Unit_Price])", stored: true);
        modelBuilder.Entity<Payroll>().Property(x => x.Net_Salary)
            .HasComputedColumnSql("([Base_Salary]+[Bonus]-[Deduction])", stored: true);
        modelBuilder.Entity<TopUpReceipt>().Property(x => x.Receipt_Code)
            .HasComputedColumnSql("('RCP-'+CONVERT(varchar(8),[Trans_Date],112)+'-'+CONVERT(varchar(10),[Transaction_ID]))", stored: true);
        modelBuilder.Entity<TopUpReceipt>().Property(x => x.Total_Credited)
            .HasComputedColumnSql("([Paid_Amount]+[Bonus_Amount])", stored: true);
        modelBuilder.Entity<TopUpReceipt>().Property(x => x.Balance_After)
            .HasComputedColumnSql("([Balance_Before]+[Paid_Amount]+[Bonus_Amount])", stored: true);

        modelBuilder.Entity<Position>().HasOne<Department>().WithMany().HasForeignKey(x => x.Department_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Employee>().HasOne<Position>().WithMany().HasForeignKey(x => x.Position_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Survey>().HasOne<Employee>().WithMany().HasForeignKey(x => x.Created_By).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Customer>().HasOne<MembershipTier>().WithMany().HasForeignKey(x => x.Tier_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<SurveyQuestion>().HasOne<Survey>().WithMany().HasForeignKey(x => x.Survey_ID).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UsageSession>().HasOne<Customer>().WithMany().HasForeignKey(x => x.Customer_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<UsageSession>().HasOne<Computer>().WithMany().HasForeignKey(x => x.Computer_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<UsageSession>().HasOne<Employee>().WithMany().HasForeignKey(x => x.Employee_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Order>().HasOne<Customer>().WithMany().HasForeignKey(x => x.Customer_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Order>().HasOne<Computer>().WithMany().HasForeignKey(x => x.Computer_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Order>().HasOne<Employee>().WithMany().HasForeignKey(x => x.Employee_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Product>().HasOne<ProductCategory>().WithMany().HasForeignKey(x => x.Category_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<InventoryTransaction>().HasOne<Product>().WithMany().HasForeignKey(x => x.Product_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<InventoryTransaction>().HasOne<Employee>().WithMany().HasForeignKey(x => x.Employee_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<WorkSchedule>().HasOne<Employee>().WithMany().HasForeignKey(x => x.Employee_ID).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WorkSchedule>().HasOne<WorkShift>().WithMany().HasForeignKey(x => x.Shift_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<SurveyResponse>().HasOne<Survey>().WithMany().HasForeignKey(x => x.Survey_ID).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SurveyResponse>().HasOne<Customer>().WithMany().HasForeignKey(x => x.Customer_ID).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SurveyResponse>().HasOne<SurveyQuestion>().WithMany().HasForeignKey(x => x.Question_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<FinancialTransaction>().HasOne<Customer>().WithMany().HasForeignKey(x => x.Customer_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<FinancialTransaction>().HasOne<Employee>().WithMany().HasForeignKey(x => x.Processed_By).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<FinancialTransaction>().HasOne<Order>().WithMany().HasForeignKey(x => x.Order_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<FinancialTransaction>().HasOne<UsageSession>().WithMany().HasForeignKey(x => x.Session_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<FinancialTransaction>().HasOne<Combo>().WithMany().HasForeignKey(x => x.Combo_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<TopUpReceipt>().HasOne<FinancialTransaction>().WithMany().HasForeignKey(x => x.Transaction_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<TopUpReceipt>().HasOne<Customer>().WithMany().HasForeignKey(x => x.Customer_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<TopUpReceipt>().HasOne<Employee>().WithMany().HasForeignKey(x => x.Processed_By).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<TopUpReceipt>().HasOne<Combo>().WithMany().HasForeignKey(x => x.Combo_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Feedback>().HasOne<Customer>().WithMany().HasForeignKey(x => x.Customer_ID).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Feedback>().HasOne<Employee>().WithMany().HasForeignKey(x => x.Handled_By).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<OrderDetail>().HasOne<Order>().WithMany().HasForeignKey(x => x.Order_ID).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OrderDetail>().HasOne<Product>().WithMany().HasForeignKey(x => x.Product_ID).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Payroll>().HasOne<Employee>().WithMany().HasForeignKey(x => x.Employee_ID).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LeaveRequest>().HasOne<Employee>().WithMany().HasForeignKey(x => x.Employee_ID).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LeaveRequest>().HasOne<Employee>().WithMany().HasForeignKey(x => x.Approved_By).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Attendance>().HasOne<WorkSchedule>().WithMany().HasForeignKey(x => x.Schedule_ID).OnDelete(DeleteBehavior.Cascade);
    }
}
