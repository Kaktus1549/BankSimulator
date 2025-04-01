#!/bin/bash

clear
# ASCII art banner (decoded from base64)
base64 -d <<< "CiAgXyAgX18gICAgIF8gICAgXyAgICAgICAgICAgICBfX19fICAgICAgICAgICAgICBfICAgIAogfCB8LyAvICAgIHwgfCAgfCB8ICAgICAgICAgICB8ICBfIFwgICAgICAgICAgICB8IHwgICAKIHwgJyAvIF9fIF98IHwgX3wgfF8gXyAgIF8gX19ffCB8XykgfCBfXyBfIF8gX18gfCB8IF9fCiB8ICA8IC8gX2AgfCB8LyAvIF9ffCB8IHwgLyBfX3wgIF8gPCAvIF9gIHwgJ18gXHwgfC8gLwogfCAuIFwgKF98IHwgICA8fCB8X3wgfF98IFxfXyBcIHxfKSB8IChffCB8IHwgfCB8ICAgPCAKIHxffFxfXF9fLF98X3xcX1xcX198XF9fLF98X19fL19fX18vIFxfXyxffF98IHxffF98XF9cCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAK"
echo "Welcome to the KaktusBank 2025 installation script!"
echo "This script will guide you through the installation process."
echo "Please make sure you have Docker and Docker Compose installed."
echo "If you want to leave the default values, just press Enter."
echo ""

#########################
# Database configuration
#########################
echo "Database configuration"
echo "----------------------"
echo ""

read -p "Enter database user (default: kaktus): " DATABASE_USER
DATABASE_USER=${DATABASE_USER:-kaktus}
read -sp "Enter database password (default: heslo): " DATABASE_PASSWORD
DATABASE_PASSWORD=${DATABASE_PASSWORD:-heslo}
echo ""
read -p "Enter database port (default: 3306): " DATABASE_PORT
DATABASE_PORT=${DATABASE_PORT:-3306}

clear
# Optional banner for next section
base64 -d <<< "CiAgXyAgX18gICAgIF8gICAgXyAgICAgICAgICAgICBfX19fICAgICAgICAgICAgICBfICAgIAogfCB8LyAvICAgIHwgfCAgfCB8ICAgICAgICAgICB8ICBfIFwgICAgICAgICAgICB8IHwgICAKIHwgJyAvIF9fIF98IHwgX3wgfF8gXyAgIF8gX19ffCB8XykgfCBfXyBfIF8gX18gfCB8IF9fCiB8ICA8IC8gX2AgfCB8LyAvIF9ffCB8IHwgLyBfX3wgIF8gPCAvIF9gIHwgJ18gXHwgfC8gLwogfCAuIFwgKF98IHwgICA8fCB8X3wgfF98IFxfXyBcIHxfKSB8IChffCB8IHwgfCB8ICAgPCAKIHxffFxfXF9fLF98X3xcX1xcX198XF9fLF98X19fL19fX18vIFxfXyxffF98IHxffF98XF9cCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAK"
echo "Docker & Port configuration"
echo "---------------------------"
echo ""

read -p "Enter Docker backend host port (default: 5000): " DOCKER_WEB_PORT
DOCKER_WEB_PORT=${DOCKER_WEB_PORT:-5000}
read -p "Enter Docker frontend host port (default: 3000): " DOCKER_FRONTEND_PORT
DOCKER_FRONTEND_PORT=${DOCKER_FRONTEND_PORT:-3000}

clear
base64 -d <<< "CiAgXyAgX18gICAgIF8gICAgXyAgICAgICAgICAgICBfX19fICAgICAgICAgICAgICBfICAgIAogfCB8LyAvICAgIHwgfCAgfCB8ICAgICAgICAgICB8ICBfIFwgICAgICAgICAgICB8IHwgICAKIHwgJyAvIF9fIF98IHwgX3wgfF8gXyAgIF8gX19ffCB8XykgfCBfXyBfIF8gX18gfCB8IF9fCiB8ICA8IC8gX2AgfCB8LyAvIF9ffCB8IHwgLyBfX3wgIF8gPCAvIF9gIHwgJ18gXHwgfC8gLwogfCAuIFwgKF98IHwgICA8fCB8X3wgfF98IFxfXyBcIHxfKSB8IChffCB8IHwgfCB8ICAgPCAKIHxffFxfXF9fLF98X3xcX1xcX198XF9fLF98X19fL19fX18vIFxfXyxffF98IHxffF98XF9cCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAK"
echo "Backend configuration"
echo "----------------------"
echo ""

read -sp "Enter JWT secret (default: None): " JWT_SECRET
JWT_SECRET=${JWT_SECRET:-}
echo ""
read -p "Enter JWT issuer (default: SvecovinaBanka): " JWT_ISSUER
JWT_ISSUER=${JWT_ISSUER:-SvecovinaBanka}
read -p "Enter maximum debt allowed (default: 5000): " MAX_DEBT
MAX_DEBT=${MAX_DEBT:-5000}
read -p "Enter maximum student withdrawal (default: 1000): " MAX_STUDENT_WITHDRAWAL
MAX_STUDENT_WITHDRAWAL=${MAX_STUDENT_WITHDRAWAL:-1000}
read -p "Enter maximum student daily withdrawal (default: 250): " MAX_STUDENT_DAILY_WITHDRAWAL
MAX_STUDENT_DAILY_WITHDRAWAL=${MAX_STUDENT_DAILY_WITHDRAWAL:-250}
read -p "Enter interest rate (default: 0.02): " INTEREST_RATE
INTEREST_RATE=${INTEREST_RATE:-0.02}

clear

cat <<EOF > ./Docker/.env
# Database configuration
DATABASE_USER=$DATABASE_USER
DATABASE_PASSWORD=$DATABASE_PASSWORD
DATABASE_PORT=$DATABASE_PORT
# Docker & Port configuration
DOCKER_WEB_PORT=$DOCKER_WEB_PORT
DOCKER_FRONTEND_PORT=$DOCKER_FRONTEND_PORT
EOF

cat <<EOF > ./Backend/.env
# Backend configuration
JWT_SECRET=$JWT_SECRET
JWT_ISSUER=$JWT_ISSUER
MAX_DEBT=$MAX_DEBT
MAX_STUDENT_WITHDRAWAL=$MAX_STUDENT_WITHDRAWAL
MAX_STUDENT_DAILY_WITHDRAWAL=$MAX_STUDENT_DAILY_WITHDRAWAL
INTEREST_RATE=$INTEREST_RATE
EOF

echo "Configuration files created successfully!"

cd Docker
echo "Starting Docker containers..."
docker compose up --build -d
echo "Docker containers started successfully!"