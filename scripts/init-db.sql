-- AutoDocOps Database Initialization Script
-- This script sets up the initial database structure and data

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Create schemas
CREATE SCHEMA IF NOT EXISTS autodocops;
CREATE SCHEMA IF NOT EXISTS audit;

-- Set default schema
SET search_path TO autodocops, public;

-- Create audit table for tracking changes
CREATE TABLE IF NOT EXISTS audit.audit_log (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    table_name VARCHAR(100) NOT NULL,
    operation VARCHAR(10) NOT NULL,
    old_values JSONB,
    new_values JSONB,
    user_id UUID,
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_audit_log_table_name ON audit.audit_log(table_name);
CREATE INDEX IF NOT EXISTS idx_audit_log_timestamp ON audit.audit_log(timestamp);
CREATE INDEX IF NOT EXISTS idx_audit_log_user_id ON audit.audit_log(user_id);

-- Create function for audit logging
CREATE OR REPLACE FUNCTION audit.log_changes()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        INSERT INTO audit.audit_log (table_name, operation, old_values)
        VALUES (TG_TABLE_NAME, TG_OP, row_to_json(OLD));
        RETURN OLD;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO audit.audit_log (table_name, operation, old_values, new_values)
        VALUES (TG_TABLE_NAME, TG_OP, row_to_json(OLD), row_to_json(NEW));
        RETURN NEW;
    ELSIF TG_OP = 'INSERT' THEN
        INSERT INTO audit.audit_log (table_name, operation, new_values)
        VALUES (TG_TABLE_NAME, TG_OP, row_to_json(NEW));
        RETURN NEW;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- Create initial admin user (for development only)
-- In production, this should be handled by the application
DO $$
BEGIN
    -- This is just a placeholder for development
    -- Real user management should be handled by Supabase Auth
    RAISE NOTICE 'Database initialized successfully for AutoDocOps';
END $$;

-- Grant permissions
GRANT USAGE ON SCHEMA autodocops TO postgres;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA autodocops TO postgres;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA autodocops TO postgres;

-- Set up Row Level Security (RLS) policies will be added by Entity Framework migrations
-- This is just the foundation

