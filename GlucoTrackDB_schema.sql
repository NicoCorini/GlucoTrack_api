/*
====================================================================================
GlucoTrackDB - Database for Glycemic and Therapeutic Monitoring
====================================================================================

Overview
--------
GlucoTrackDB is a relational database designed to support the clinical and self-management of diabetic patients. It enables the tracking of therapies, medication intakes (scheduled and unscheduled), glycemic measurements, symptoms, comorbidities, risk factors, and clinical alerts. The model is flexible and extensible, suitable for both healthcare professionals and patients.

Main Entities
-------------
- Roles: User roles (Admin, Doctor, Patient).
- Users: Unified registry for all users (doctors, patients, admins) with personal and professional data.
- PatientDoctors: Many-to-many relationships between patients and doctors, with validity dates.
- RiskFactors & PatientRiskFactors: General risk factors and their association to patients.
- ClinicalComorbidities: long-term chronic conditions diagnosed and managed by the doctor.
- Therapies: Prescribed therapies, linked to both doctor and patient, with instructions and dates.
- MedicationSchedules: Detailed scheduling of medication intakes for each therapy.
- MedicationIntakes: Records of actual medication intakes, both scheduled and unscheduled.
- MeasurementTypes & MealTypes: Fixed types for glycemic measurements and meals (e.g., pre/post meal, breakfast/lunch/dinner).
- GlycemicMeasurements: Glycemic values measured by patients, with details on type and meal.
- Symptoms: Symptoms reported by patients, with description and timestamp.
- ReportedConditions: temporary or acute conditions reported by the patient.
- AlertTypes, Alerts, AlertRecipients: Clinical alert system, with types, messages, and recipients (patients or doctors).
- ChangeLogs: Audit log of clinical data changes made by doctors.

How to Query the Database
-------------------------
- To get a patient's clinical situation, start from Users (UserId = PatientId), then:
    - Doctor relationships: PatientDoctors
    - Active therapies: Therapies
    - Medication schedules and intakes: MedicationSchedules, MedicationIntakes
    - Glycemic measurements: GlycemicMeasurements
    - Symptoms and conditions: Symptoms, ReportedConditions
    - Risk factors: PatientRiskFactors
    - Received alerts: AlertRecipients (linked to Alerts)
- To see therapies prescribed by a doctor, filter Therapies by DoctorId.
- Alerts are generated in Alerts and delivered via AlertRecipients.
- All tables are linked by foreign keys to ensure referential integrity.

Usage Scenarios
---------------
- Clinical dashboards for doctors and patients
- Automated alerting and notification systems
- Data analytics and reporting on glycemic control, therapy adherence, and risk factors
- Integration with mobile or web applications for diabetes management

Sample Data
-----------
The database includes realistic sample data:
- Roles: Admin, Doctor, Patient
- Users: 3 doctors, 3 patients, 1 admin
- Patient-doctor relationships: each patient has at least one doctor, some have more
- Risk factors: 5 general factors, associated with patients
- Meal and measurement types: breakfast/lunch/dinner, pre/post meal
- Alert types and sample alerts: various clinical alerts, with different recipients
- Therapies: each patient has an active therapy
- Medication schedules and intakes: both scheduled and unscheduled
- Glycemic measurements: pre and post meal values for each patient
- Symptoms: various symptoms reported by patients
- Bulk data: daily glycemic measurements, medication intakes, and symptoms for realistic testing

Best Practices
--------------
- Always use foreign keys to join related tables (e.g., Users, Therapies, Alerts)
- Use the sample data to test queries, procedures, and application interfaces
- Extend the model as needed for additional clinical or administrative requirements

This guide is intended to help developers, analysts, and clinicians understand and use GlucoTrackDB efficiently for diabetes management and research.
*/

-- Create database
CREATE DATABASE GlucoTrackDB;

USE GlucoTrackDB;

-- Roles
CREATE TABLE Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName VARCHAR(255) NOT NULL UNIQUE
);

-- Users
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    FirstName VARCHAR(255) NULL,
    LastName VARCHAR(255) NULL,
    Email VARCHAR(255) NOT NULL UNIQUE,
    RoleId INT NOT NULL FOREIGN KEY REFERENCES Roles(RoleId),
    -- Patient Data
    BirthDate DATE NULL,
    Height NUMERIC(5,2) NULL,
    Weight NUMERIC(5,2) NULL,
    FiscalCode VARCHAR(16) NULL,
    Gender NVARCHAR(100) NULL,
    -- Doctor Data
    Specialization VARCHAR(255) NULL,
    AffiliatedHospital VARCHAR(255) NULL,
    -- Audit
    CreatedAt DATETIME NULL,
    LastAccess DATETIME NULL
);

-- Doctor-Patient Relationship
CREATE TABLE PatientDoctors (
    PatientDoctorId INT IDENTITY(1,1) PRIMARY KEY,
    PatientId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    DoctorId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    StartDate DATE NOT NULL,
    EndDate DATE NULL
);

-- Risk Factors
CREATE TABLE RiskFactors (
    RiskFactorId INT IDENTITY(1,1) PRIMARY KEY,
    Label VARCHAR(255) NOT NULL UNIQUE,
    Description TEXT NULL
);

CREATE TABLE PatientRiskFactors (
    PatientRiskFactorId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    RiskFactorId INT NOT NULL FOREIGN KEY REFERENCES RiskFactors(RiskFactorId)
);

-- Comorbidities
CREATE TABLE ClinicalComorbidities (
    ClinicalComorbidityId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    Comorbidity VARCHAR(255) NULL,
    StartDate DATE NULL,
    EndDate DATE NULL
);

-- Therapies
CREATE TABLE Therapies (
    TherapyId INT IDENTITY(1,1) PRIMARY KEY,
    DoctorId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    Title TEXT NOT NULL,
    Instructions TEXT NULL,
    StartDate DATE NULL,
    EndDate DATE NULL,
    PreviousTherapyId INT NULL FOREIGN KEY REFERENCES Therapies(TherapyId),
    CreatedAt DATETIME NULL
);

-- Medication Schedules
CREATE TABLE MedicationSchedules (
    MedicationScheduleId INT IDENTITY(1,1) PRIMARY KEY,
    TherapyId INT NOT NULL FOREIGN KEY REFERENCES Therapies(TherapyId),
    MedicationName VARCHAR(255) NOT NULL,
    DailyIntakes INT NOT NULL,
    Quantity NUMERIC(8,2) NOT NULL,
    Unit VARCHAR(10) NOT NULL
);

-- Medication Intakes
CREATE TABLE MedicationIntakes (
    MedicationIntakeId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    MedicationScheduleId INT NULL FOREIGN KEY REFERENCES MedicationSchedules(MedicationScheduleId),
    IntakeDateTime DATETIME NOT NULL,
    ExpectedQuantityValue NUMERIC(8,2) NOT NULL,
    Unit VARCHAR(10) NOT NULL,
    Note TEXT NULL,
    MedicationTakenName VARCHAR(255) NULL
);

-- Measurement and Meal Types
CREATE TABLE MeasurementTypes (
    MeasurementTypeId INT IDENTITY(1,1) PRIMARY KEY,
    Label VARCHAR(255) NOT NULL UNIQUE,
    Description TEXT NULL
);

CREATE TABLE MealTypes (
    MealTypeId INT IDENTITY(1,1) PRIMARY KEY,
    Label VARCHAR(255) NOT NULL UNIQUE,
    Description TEXT NULL
);

-- Glycemic Measurements
CREATE TABLE GlycemicMeasurements (
    GlycemicMeasurementId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    MeasurementDateTime DATETIME NOT NULL,
    MeasurementTypeId INT NOT NULL FOREIGN KEY REFERENCES MeasurementTypes(MeasurementTypeId),
    MealTypeId INT NOT NULL FOREIGN KEY REFERENCES MealTypes(MealTypeId),
    Value SMALLINT NOT NULL,
    Note TEXT NULL
);

-- Symptoms
CREATE TABLE Symptoms (
    SymptomId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    Description VARCHAR(255) NULL,
    OccurredAt DATETIME NOT NULL
);

-- Reported Conditions
CREATE TABLE ReportedConditions (
    ConditionId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    Description VARCHAR(255) NULL,
    StartDate DATETIME NULL,
    EndDate DATETIME NULL
);

-- Alert Types
CREATE TABLE AlertTypes (
    AlertTypeId INT IDENTITY(1,1) PRIMARY KEY,
    Label VARCHAR(50) NOT NULL UNIQUE,
    Description TEXT NULL
);

-- Alerts
CREATE TABLE Alerts (
    AlertId INT IDENTITY(1,1) PRIMARY KEY,
    AlertTypeId INT NOT NULL FOREIGN KEY REFERENCES AlertTypes(AlertTypeId),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    Message TEXT NOT NULL,
    CreatedAt DATETIME NULL,
    ReferenceDate DATE NULL,
    ReferencePeriod VARCHAR(50) NULL,
    ReferenceObjectId INT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'open',
    ResolvedAt DATETIME NULL
);

CREATE TABLE AlertRecipients (
    AlertRecipientId INT IDENTITY(1,1) PRIMARY KEY,
    AlertId INT NOT NULL FOREIGN KEY REFERENCES Alerts(AlertId),
    RecipientUserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    IsRead BIT NULL DEFAULT 0,
    ReadAt DATETIME NULL,
    NotifiedAt DATETIME NULL
);

-- Change Log
CREATE TABLE ChangeLogs (
    ChangeLogId INT IDENTITY(1,1) PRIMARY KEY,
    DoctorId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    TableName VARCHAR(255) NOT NULL,
    RecordId INT NOT NULL,
    Action VARCHAR(20) NOT NULL CHECK (Action IN ('INSERT', 'UPDATE', 'DELETE')),
    Timestamp DATETIME NOT NULL DEFAULT GETDATE(),
    DetailsBefore NVARCHAR(MAX) NULL,
    DetailsAfter NVARCHAR(MAX) NULL
);