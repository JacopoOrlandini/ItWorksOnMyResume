-- ItWorksOnMyResume — PostgreSQL initialization
-- Runs automatically on first container start
-- File: infra/postgres/init.sql

-- ─────────────────────────────────────────
-- Extensions
-- ─────────────────────────────────────────
CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS pg_trgm;   -- fuzzy text search on skill names

-- ─────────────────────────────────────────
-- Schema
-- ─────────────────────────────────────────
CREATE TABLE IF NOT EXISTS users (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username        VARCHAR(50)  NOT NULL UNIQUE,
    email           VARCHAR(255) NOT NULL UNIQUE,
    password_hash   TEXT         NOT NULL,
    bio             TEXT,
    avatar_key      TEXT,                          -- MinIO object key
    latitude        DOUBLE PRECISION,
    longitude       DOUBLE PRECISION,
    reputation_score DECIMAL(4,2) NOT NULL DEFAULT 0.00,
    exchanges_count  INT          NOT NULL DEFAULT 0,
    is_active       BOOLEAN      NOT NULL DEFAULT true,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS skill_categories (
    id      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name    VARCHAR(100) NOT NULL UNIQUE,
    slug    VARCHAR(100) NOT NULL UNIQUE,
    icon    VARCHAR(50)                            -- icon name for the app
);

CREATE TABLE IF NOT EXISTS user_skills (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id     UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    category_id UUID NOT NULL REFERENCES skill_categories(id),
    type        VARCHAR(10) NOT NULL CHECK (type IN ('offer', 'seek')),
    level       SMALLINT    NOT NULL CHECK (level BETWEEN 1 AND 5),
    description TEXT,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS exchanges (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_a_id   UUID NOT NULL REFERENCES users(id),
    user_b_id   UUID NOT NULL REFERENCES users(id),
    skill_a     TEXT NOT NULL,                     -- what user_a offers
    skill_b     TEXT NOT NULL,                     -- what user_b offers
    status      VARCHAR(20) NOT NULL DEFAULT 'pending'
                    CHECK (status IN ('pending','accepted','completed','cancelled')),
    proposed_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    accepted_at  TIMESTAMPTZ,
    completed_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS messages (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    exchange_id UUID NOT NULL REFERENCES exchanges(id) ON DELETE CASCADE,
    sender_id   UUID NOT NULL REFERENCES users(id),
    body        TEXT NOT NULL,
    sent_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS reviews (
    id           UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    exchange_id  UUID NOT NULL REFERENCES exchanges(id),
    reviewer_id  UUID NOT NULL REFERENCES users(id),
    reviewee_id  UUID NOT NULL REFERENCES users(id),
    score        SMALLINT NOT NULL CHECK (score BETWEEN 1 AND 5),
    comment      TEXT,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (exchange_id, reviewer_id)              -- one review per side per exchange
);

-- ─────────────────────────────────────────
-- Indexes
-- ─────────────────────────────────────────

-- Geo: partial index on coordinates (PostGIS query done via Dapper with ST_MakePoint)
CREATE INDEX IF NOT EXISTS idx_users_location
    ON users (latitude, longitude)
    WHERE latitude IS NOT NULL AND longitude IS NOT NULL;

-- Skill search by category
CREATE INDEX IF NOT EXISTS idx_user_skills_category
    ON user_skills (category_id, type);

CREATE INDEX IF NOT EXISTS idx_user_skills_user
    ON user_skills (user_id);

-- Fuzzy search on skill category names
CREATE INDEX IF NOT EXISTS idx_skill_categories_name_trgm
    ON skill_categories USING GIN (name gin_trgm_ops);

-- Exchange lookups
CREATE INDEX IF NOT EXISTS idx_exchanges_user_a ON exchanges (user_a_id);
CREATE INDEX IF NOT EXISTS idx_exchanges_user_b ON exchanges (user_b_id);
CREATE INDEX IF NOT EXISTS idx_exchanges_status  ON exchanges (status);

-- Message history per exchange
CREATE INDEX IF NOT EXISTS idx_messages_exchange
    ON messages (exchange_id, sent_at DESC);

-- Reviews per user
CREATE INDEX IF NOT EXISTS idx_reviews_reviewee
    ON reviews (reviewee_id);

-- ─────────────────────────────────────────
-- Seed: skill categories
-- ─────────────────────────────────────────
INSERT INTO skill_categories (name, slug, icon) VALUES
    ('Programming',       'programming',       'code'),
    ('Design',            'design',            'palette'),
    ('Music',             'music',             'music-note'),
    ('Languages',         'languages',         'translate'),
    ('Cooking',           'cooking',           'chef-hat'),
    ('Photography',       'photography',       'camera'),
    ('Fitness',           'fitness',           'dumbbell'),
    ('Carpentry',         'carpentry',         'hammer'),
    ('Writing',           'writing',           'pencil'),
    ('Mathematics',       'mathematics',       'calculator'),
    ('Electronics',       'electronics',       'circuit'),
    ('Gardening',         'gardening',         'leaf')
ON CONFLICT (slug) DO NOTHING;

-- ─────────────────────────────────────────
-- Updated_at trigger
-- ─────────────────────────────────────────
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();