-- Create per-service databases (Database-per-Service pattern)
CREATE DATABASE fleet_vehicle;
CREATE DATABASE fleet_driver;
CREATE DATABASE fleet_gps;
CREATE DATABASE fleet_trip;
CREATE DATABASE fleet_audit;
CREATE DATABASE fleet_predictive_maintenance;

-- Enable extensions on fleet_gps (Geospatial & Time-series)
\c fleet_gps
CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- Enable extensions on fleet_predictive_maintenance (Sensor time-series)
\c fleet_predictive_maintenance
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- Enable UUID generation extension on standard relational databases
\c fleet_vehicle
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

\c fleet_driver
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

\c fleet_trip
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

\c fleet_audit
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
