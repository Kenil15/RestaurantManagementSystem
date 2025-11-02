Restaurant Ordering System (Portfolio Edition)
================================================

Modern ASP.NET Core MVC app demonstrating a food ordering flow with Identity, menus, cart, checkout, orders, and a sleek tracking UI.

Highlights (Upwork-ready)
- User auth with long-lived sessions; customer and admin roles
- Menu browsing with availability badges and lazy-loaded images
- Cart with optional upsell (cold drink)
- Checkout with payment method selection (demo) and address handling
- Orders: list, details, cancel (customer), status update (admin)
- Order tracking UI: progress stepper, ETA, driver info (demo) and chatbot widget
- Performance: response compression, caching, static cache headers

Getting Started
1. Requirements: SQL Server LocalDB/SQL Server, .NET 8 SDK
2. Configure connection string in appsettings.json if needed
3. First run auto-migrates and seeds data
4. Start the app

Windows PowerShell
```
cd ForUpworkRestaurentManagement
dotnet restore
dotnet run
```

- Admin: admin@restaurant.com / Admin123!
- Register as a customer to place orders

Portfolio Demo Script
1) Home: Explore restaurant cards and call-to-action
2) Menu: See "Not available" badge; add item with upsell
3) Cart: Adjust quantity
4) Checkout: Place order (Cash on Delivery)
5) Orders: See new order, open details, tracking UI; cancel when pending
6) Admin: Update status to see the tracker advance

Notes
- Payments are demo only; integrate Stripe/Razorpay for production
- Tracker map is a placeholder; plug in Leaflet/Mapbox and real driver events for live tracking

