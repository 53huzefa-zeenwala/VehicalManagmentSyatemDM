# Client clarifications — received 2026-11-18

Verbatim client response, relayed by the project owner. It takes precedence over the Staff UI mockup wherever the two differ.

---

# Vehicle Management Application – Functional Requirements & Clarifications

### 1. Staff Assignment and Responsibilities

The Admin will create and manage all Staff accounts.

Each staff member will have a dedicated profile. The staff member's responsibilities and access will be limited to the following:

- View their own profile.
- View details of the vehicles they have started.
- View the Parking Lot details from which a vehicle was started.
- View available parking spaces across all parking lots.
- Start a vehicle when taking it out of the parking lot.
- End the vehicle session when returning it to the parking lot.

Once the staff member ends a vehicle session, the application should automatically display the complete session details, including:

- Vehicle details
- Vehicle category
- Parking Lot from which the vehicle was started
- Start date and time
- End date and time
- Total duration the vehicle remained outside the parking lot

### 2. Handling Active Vehicles and Missed End Entries

The Admin should be able to see all currently active vehicles in real time.

- The Admin can manually end any active vehicle session.
- Staff members are also responsible for ending their respective vehicle sessions when returning vehicles.
- If a staff member forgets to end a vehicle session, the application should continuously send reminders or notifications.
- An active timer should remain visible in the top navigation bar of the Staff application from the moment the vehicle is started until the session is ended.
- The timer should continue running until the vehicle is marked as Ended by either the Staff or Admin.

This will help ensure that no vehicle remains active in the system without proper tracking.

### 3. Vehicle Start and End Details

Whenever a vehicle is started or ended, the application should record all necessary information.

**Vehicle Start Details:**
- Staff Member Name and ID
- Driver Name
- Vehicle Registration Number
- Vehicle Category
- Vehicle Brand and Model
- Parking Lot Name from which the vehicle was started
- Start Date and Time

**Vehicle End Details:**
- Staff Member Name and ID
- Driver Name
- Vehicle Details
- Original Parking Lot Details
- Start Date and Time
- End Date and Time
- Total Duration the Vehicle Remained Outside

All records should be maintained in the system for future reference and reporting.

### 4. Parking Lot Capacity and Configuration

The Admin should have the option to add and manage multiple Parking Lots.

While adding a new Parking Lot, the Admin should be able to configure all necessary details, including:

- Parking Lot Type (Open Parking / Underground Parking / Other)
- Total Parking Capacity
- Vehicle Type-wise Capacity
- Available Parking Spaces
- Parking Lot Status (Active / Inactive)

The system should automatically calculate the occupied and available parking spaces based on vehicle entry and exit records.

The Admin should have complete control over Parking Lot configuration and capacity management.

### 5. Parking Lot Details

The following information should be available while creating or managing a Parking Lot:

1. Parking Lot Name
2. Parking Lot Code / Unique ID
3. Full Address
4. Google Maps Location Pin
5. Contact Person Name
6. Contact Number
7. Operating Hours (Opening and Closing Time)
8. Vehicle Type-wise Capacity
9. Total Parking Capacity
10. Parking Lot Status (Active / Inactive)

The application should also provide a Map View to display all registered Parking Lot locations.

### 6. Vehicle Details

The Admin should be able to add and manage vehicle information with the following details:

1. Vehicle Registration Number (Number Plate)
2. Vehicle Type (Car, Garbage Truck, Ambulance, etc.)
3. Vehicle Brand
4. Vehicle Model
5. Vehicle Color
6. Vehicle Photo

These details should be available whenever a vehicle is started or ended.

### 7. Admin and Staff Login System

**Admin Login:**
- A dedicated Admin account will be created.
- The Admin username/ID and password will be configured and provided by us.
- The Admin will have complete access to manage the application, including Parking Lots, Staff, Vehicles, and Reports.

**Staff Login:**
- The Admin will create individual accounts for each Staff member.
- Every Staff member will receive a unique Staff ID and password from the Admin.
- Each Staff ID will be unique and used for login.

**Example Staff ID:** `DM324DU2131`

Staff members will log in using their assigned unique Staff ID and password.

Each staff member will have access only to the permitted Staff functionalities, while the Admin will have complete control over the application.

---

**Note:** All vehicle activities, parking records, staff activities, and start/end timestamps should be stored systematically to maintain accurate records and provide complete tracking and reporting functionality.
