# DBProjectMaui

## Introduction
This project is a sample implementation of managing a 1:n relation between two tables in a database using Microsoft.Maui.Controls. It allows CRUD (Create, Read, Update, Delete) operations on child entities associated with a selected parent entity. The application is designed to work with databases that have a 1:n relationship between two tables, where the child table has a single foreign key referencing the parent table.

## Features
- Connects to a SQL database and retrieves data
- Displays parent entities in a list view
- When a parent entity is selected, displays associated child entities
- Supports CRUD operations on child entities:
  - Insert new child entities
  - Update existing child entities
  - Delete child entities

## Usage
1. Ensure you have Microsoft.Maui.Controls set up in your development environment.
2. Clone the repository to your local machine.
3. Open the solution in your preferred IDE.
4. Modify the connection string in `MainPage.xaml.cs` to point to your database.
5. Run the application.
6. Click on a parent entity to see its associated child entities.
7. Perform CRUD operations on child entities as needed.

## Prerequisites
- Microsoft.Maui.Controls
- .NET development environment
- Access to a SQL database with two tables having a 1:n relationship

## Compatibility
This application is designed to work with almost all databases that have two tables in a 1:n relationship, where the child table has a single foreign key.

## Notes
- This project assumes a basic understanding of Microsoft.Maui.Controls and SQL databases.
- Ensure that proper error handling and validation are implemented before deploying to production environments.

## License
This project is licensed under the [MIT License](LICENSE).
