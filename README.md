# Budget - Personal Finance Management Core Library

A comprehensive C# library for personal and family budget management, designed following MVP (Model-View-Presenter) architecture patterns.

## 🎯 Project Overview

This project serves as the **Model layer** in an MVP architecture, containing core business logic and data models for budget management. It will be compiled as a **DLL library** for use in various presentation layers (WPF, Web, Mobile, etc.).

## 🏗️ Architecture

```
Budget/
├── Models/           # Data models and DTOs
├── Services/         # Business logic layer  
├── Utils/           # Utility classes
└── data/            # Test data files
```

### Models Layer
- **Category.cs** - Financial category definitions (Income, Expense, Investment, Debt, Savings)
- **Transaction.cs** - Individual financial transactions
- **BudgetItem.cs** - Aggregated view models for reporting

### Services Layer
- **CategoryService.cs** - Category management with database operations
- **TransactionService.cs** - Transaction management with database operations  
- **AuthService.cs** - User authentication service
- **DatabaseService.cs** - Core database connection and operations

### Core Controller
- **HomeBudget.cs** - Main facade providing unified API access

## 💰 Supported Financial Categories

### Transaction Types
- **Income** - Salary, rental income, etc.
- **Expense** - Daily spending across multiple categories
- **Investment** - Stocks, funds, real estate investment
- **Debt** - Mortgage, auto loans, credit card payments
- **Savings** - Emergency funds and savings accounts

### Expense Categories
- Utilities, Food & Dining, Transportation
- Health & Personal Care, Insurance, Clothes
- Education, Vacation, Social Expenses
- Municipal & School Tax, Rental Expenses
- Miscellaneous

## 🚀 Key Features

### Data Management
- **SQLite database** - Lightweight, embedded database solution
- **CRUD operations** - Full create, read, update, delete support
- **ACID transactions** - Data consistency and integrity
- **Relational design** - Normalized database schema

### Reporting & Analytics
- **Time-based filtering** - Filter by date ranges
- **Category analysis** - Group by categories with totals
- **Monthly summaries** - Aggregate data by month
- **Running balances** - Track account balance over time
- **Cross-category reporting** - Complex queries with multiple dimensions

### Advanced Queries
```csharp
// Get filtered budget items
List<BudgetItem> items = budget.GetBudgetItems(startDate, endDate, filterFlag, categoryId);

// Monthly grouping
List<BudgetItemsByMonth> monthlyData = budget.GetBudgetItemsByMonth(start, end, filter, catId);

// Category analysis  
List<BudgetItemsByCategory> categoryData = budget.GetBudgetItemsByCategory(start, end, filter, catId);
```

## 🛠️ Technology Stack

- **.NET Framework/Core** - C# 
- **SQLite** - Embedded database for data persistence
- **Entity Framework Core** - ORM for database operations
- **LINQ** - Advanced querying capabilities
- **Decimal precision** - Financial calculations using decimal type

## 📦 Usage as DLL

This library is designed to be compiled as a DLL and consumed by various presentation layers:

```csharp
// Basic usage example
var budget = new HomeBudget("budget.db");
budget.LoadDatabase();

// Add a transaction
budget.transactions.Add(DateTime.Now, categoryId, 50.00m, "Grocery shopping");

// Generate monthly report with database queries
var monthlyReport = budget.GetBudgetItemsByMonth(DateTime.Now.AddMonths(-1), DateTime.Now, false, 0);
```

## 📁 Database Schema

The system uses SQLite database with the following main tables:

### Tables Structure
- **Categories** - Financial category definitions with types
- **Transactions** - Individual financial transaction records  
- **Users** - User authentication information (if enabled)

### Key Features
- **Foreign key relationships** - Data integrity between tables
- **Indexed columns** - Optimized query performance
- **Date-based partitioning** - Efficient time-range queries

## 🔒 Security Features

- **Password hashing** - Secure password storage
- **Input validation** - Data integrity checks
- **Exception handling** - Robust error management

## 🧪 Testing

Test database files are included in the `data/` folder:
- `test.db` - Sample SQLite database with test data
- Contains sample categories, transactions, and user data

## 🎯 Future Enhancements

This core library is designed to support multiple presentation layers:
- **WPF Desktop Application** - Rich desktop experience
- **Web Application** - Browser-based access
- **Mobile Apps** - iOS/Android applications
- **API Services** - RESTful web services

## 📄 License

Released under the GNU General Public License

## 🤝 Contributing

This project follows standard C# coding conventions and MVP architectural patterns. When contributing:
- Follow existing naming conventions
- Include XML documentation for public APIs
- Ensure proper exception handling
- Add appropriate unit tests

---

*This library provides the foundation for comprehensive personal finance management, designed for flexibility and reusability across multiple application types.*
