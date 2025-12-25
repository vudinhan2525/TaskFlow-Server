import XLSX from "xlsx";
import { writeFile } from "fs/promises";
import { fileURLToPath } from "url";
import { dirname, join } from "path";

// 📁 Xác định thư mục chứa file script
const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

// 📄 Đường dẫn file input/output
const INPUT_FILE = join(__dirname, "Copy of Testcases(1).xlsx");
const OUTPUT_FILE = join(__dirname, "Copy of Testcases(1)-Updated.xlsx");

// 🔢 Dữ liệu test cases (đã dán đầy đủ)
const testCases = [
  // UC1: Register Account
  {
    category: "UC1: Register Account",
    id: "REG-01",
    description: "Register with valid information",
    prerequisites: "Email is not registered in the system",
    steps: [
      {
        step: 1,
        action: "Navigate to Sign Up page",
        expected: "Registration form displays with Email, Password, and Confirm Password fields",
      },
      {
        step: 2,
        action: "Enter valid email (e.g., user@example.com)",
        expected: "Email field accepts input without error",
      },
      {
        step: 3,
        action: "Enter valid password (min 8 chars, contains letter and number)",
        expected: "Password field accepts input, shows strength indicator",
      },
      {
        step: 4,
        action: "Enter matching password in Confirm Password field",
        expected: "Confirm Password field accepts input, no mismatch error",
      },
      { step: 5, action: "Click 'Sign Up' button", expected: "Request is sent to server" },
    ],
    expectedResult:
      "201 Created – Account created with status 'Unverified', OTP sent to email, user redirected to verification page",
    status: "Pass",
  },
  {
    category: "UC1: Register Account",
    id: "REG-02",
    description: "Register with existing email",
    prerequisites: "Email 'existing@test.com' already exists in system",
    steps: [
      { step: 1, action: "Open Registration page", expected: "Registration form displays" },
      { step: 2, action: "Enter email 'existing@test.com'", expected: "Email field accepts input" },
      { step: 3, action: "Enter valid password and confirm password", expected: "Password fields accept input" },
      { step: 4, action: "Click 'Sign Up' button", expected: "Request is sent" },
    ],
    expectedResult: "400 Bad Request – Error message: 'Email already exists' displayed to user",
    status: "Fail",
  },
  {
    category: "UC1: Register Account",
    id: "REG-03",
    description: "Register with mismatching passwords",
    prerequisites: "None",
    steps: [
      { step: 1, action: "Navigate to Registration page", expected: "Registration form displays" },
      { step: 2, action: "Enter valid email", expected: "Email field accepts input" },
      { step: 3, action: "Enter password 'Password123'", expected: "Password field accepts input" },
      { step: 4, action: "Enter confirm password 'Password456' (different)", expected: "System detects mismatch" },
      { step: 5, action: "Click 'Sign Up' button", expected: "Validation error occurs" },
    ],
    expectedResult:
      "400 Validation Error – Error message: 'Passwords do not match' displayed below Confirm Password field",
    status: "Fail",
  },
  {
    category: "UC1: Register Account",
    id: "REG-04",
    description: "Register with invalid email format",
    prerequisites: "None",
    steps: [
      { step: 1, action: "Open Registration page", expected: "Registration form displays" },
      { step: 2, action: "Enter invalid email 'notanemail' (no @ or domain)", expected: "Email field accepts input" },
      { step: 3, action: "Enter valid password and confirm password", expected: "Password fields accept input" },
      { step: 4, action: "Click 'Sign Up' button", expected: "Client-side validation triggers" },
    ],
    expectedResult: "400 Validation Error – Error message: 'Invalid email format' displayed below email field",
    status: "Fail",
  },
  {
    category: "UC1: Register Account",
    id: "REG-05",
    description: "Register with weak password",
    prerequisites: "None",
    steps: [
      { step: 1, action: "Navigate to Registration page", expected: "Form displays" },
      { step: 2, action: "Enter valid email", expected: "Email accepted" },
      { step: 3, action: "Enter weak password '123' (less than 8 chars)", expected: "Password field accepts input" },
      { step: 4, action: "Enter same password in confirm field", expected: "Passwords match" },
      { step: 5, action: "Click 'Sign Up'", expected: "Validation occurs" },
    ],
    expectedResult: "400 Validation Error – Error: 'Password must be at least 8 characters' displayed",
    status: "Fail",
  },

  // UC2: Verify Account
  {
    category: "UC2: Verify Account",
    id: "VER-01",
    description: "Verify account with valid OTP",
    prerequisites: "User registered, OTP sent to email, account status is 'Unverified'",
    steps: [
      { step: 1, action: "Navigate to verification page", expected: "OTP input field displays with 6 digits" },
      { step: 2, action: "Check email for OTP code", expected: "Email received with valid 6-digit OTP" },
      { step: 3, action: "Enter correct OTP (e.g., 123456)", expected: "OTP field accepts all 6 digits" },
      { step: 4, action: "Click 'Verify' button", expected: "Verification request sent to server" },
    ],
    expectedResult:
      "200 OK – Account status changed to 'Verified', success message displayed, user redirected to login page",
    status: "Pass",
  },
  {
    category: "UC2: Verify Account",
    id: "VER-02",
    description: "Verify account with incorrect OTP",
    prerequisites: "User registered, valid OTP is '123456'",
    steps: [
      { step: 1, action: "Open verification page", expected: "OTP input displays" },
      { step: 2, action: "Enter incorrect OTP '999999'", expected: "OTP field accepts input" },
      { step: 3, action: "Click 'Verify' button", expected: "Request sent" },
    ],
    expectedResult: "400 Bad Request – Error message: 'Invalid OTP code' displayed, OTP field cleared, user can retry",
    status: "Fail",
  },
  {
    category: "UC2: Verify Account",
    id: "VER-03",
    description: "Verify account with expired OTP",
    prerequisites: "User registered 15 minutes ago, OTP expired after 10 minutes",
    steps: [
      { step: 1, action: "Navigate to verification page", expected: "OTP input displays" },
      { step: 2, action: "Enter the expired OTP code", expected: "OTP field accepts input" },
      { step: 3, action: "Click 'Verify' button", expected: "Request sent to server" },
      { step: 4, action: "System checks OTP expiry time", expected: "OTP is expired" },
    ],
    expectedResult:
      "400 Bad Request – Error: 'OTP has expired. Please request a new one.' with 'Resend OTP' button visible",
    status: "Fail",
  },
  {
    category: "UC2: Verify Account",
    id: "VER-04",
    description: "Verify with incomplete OTP",
    prerequisites: "User registered, OTP sent",
    steps: [
      { step: 1, action: "Open verification page", expected: "OTP input displays" },
      { step: 2, action: "Enter only 4 digits '1234' (incomplete)", expected: "OTP field shows 4 digits" },
      { step: 3, action: "Click 'Verify' button", expected: "Validation check occurs" },
    ],
    expectedResult:
      "400 Validation Error – Error: 'Please enter complete 6-digit OTP' displayed, Verify button disabled until 6 digits entered",
    status: "Fail",
  },

  // UC3: Resend OTP
  {
    category: "UC3: Resend OTP",
    id: "OTP-01",
    description: "Resend OTP for unverified account",
    prerequisites: "User account exists with status 'Unverified'",
    steps: [
      { step: 1, action: "Navigate to verification page", expected: "Page displays with 'Resend OTP' button" },
      { step: 2, action: "Click 'Resend OTP' button", expected: "Request sent to server" },
      { step: 3, action: "System generates new OTP", expected: "New OTP generated and old OTP invalidated" },
      { step: 4, action: "Check email inbox", expected: "New OTP email received" },
    ],
    expectedResult:
      "200 OK – New OTP sent to email, success message: 'OTP has been resent to your email', cooldown timer starts (60 seconds)",
    status: "Pass",
  },
  {
    category: "UC3: Resend OTP",
    id: "OTP-02",
    description: "Resend OTP for already verified account",
    prerequisites: "User account is already verified",
    steps: [
      {
        step: 1,
        action: "Try to access verification page with verified account token",
        expected: "System checks account status",
      },
      { step: 2, action: "Click 'Resend OTP' button", expected: "Request blocked" },
    ],
    expectedResult: "403 Forbidden – Error: 'Account is already verified', user redirected to dashboard",
    status: "Fail",
  },
  {
    category: "UC3: Resend OTP",
    id: "OTP-03",
    description: "Resend OTP multiple times (rate limit)",
    prerequisites: "User unverified, already requested OTP 3 times in last minute",
    steps: [
      { step: 1, action: "Open verification page", expected: "Page displays" },
      { step: 2, action: "Click 'Resend OTP' 4th time within 1 minute", expected: "Request sent" },
      { step: 3, action: "System checks rate limit", expected: "Rate limit exceeded" },
    ],
    expectedResult:
      "429 Too Many Requests – Error: 'Too many requests. Please wait 60 seconds before trying again', Resend button disabled with countdown timer",
    status: "Fail",
  },

  // UC4: Login
  {
    category: "UC4: Login",
    id: "LOG-01",
    description: "Login with valid credentials",
    prerequisites: "User account exists, verified, email: 'user@test.com', password: 'ValidPass123'",
    steps: [
      { step: 1, action: "Navigate to Login page", expected: "Login form displays with Email and Password fields" },
      { step: 2, action: "Enter email 'user@test.com'", expected: "Email field accepts input" },
      { step: 3, action: "Enter password 'ValidPass123'", expected: "Password field shows masked characters" },
      { step: 4, action: "Click 'Login' button", expected: "Login request sent to server" },
      { step: 5, action: "System validates credentials", expected: "Credentials match database" },
    ],
    expectedResult: "200 OK – JWT token generated, user authenticated, redirected to dashboard, session created",
    status: "Pass",
  },
  {
    category: "UC4: Login",
    id: "LOG-02",
    description: "Login with incorrect password",
    prerequisites: "Valid user exists with email 'user@test.com'",
    steps: [
      { step: 1, action: "Open Login page", expected: "Login form displays" },
      { step: 2, action: "Enter correct email 'user@test.com'", expected: "Email accepted" },
      { step: 3, action: "Enter wrong password 'WrongPass123'", expected: "Password field accepts input" },
      { step: 4, action: "Click 'Login' button", expected: "Authentication attempted" },
    ],
    expectedResult:
      "401 Unauthorized – Error message: 'Invalid email or password' displayed, login attempt logged, user remains on login page",
    status: "Fail",
  },
  {
    category: "UC4: Login",
    id: "LOG-03",
    description: "Login with unverified account",
    prerequisites: "User registered but not verified OTP",
    steps: [
      { step: 1, action: "Navigate to Login page", expected: "Form displays" },
      { step: 2, action: "Enter unverified account credentials", expected: "Credentials accepted" },
      { step: 3, action: "Click 'Login'", expected: "Request sent" },
      { step: 4, action: "System checks account verification status", expected: "Status is 'Unverified'" },
    ],
    expectedResult: "403 Forbidden – Error: 'Please verify your email first', link to resend verification OTP provided",
    status: "Fail",
  },
  {
    category: "UC4: Login",
    id: "LOG-04",
    description: "Login with non-existent email",
    prerequisites: "Email 'notexist@test.com' does not exist in system",
    steps: [
      { step: 1, action: "Open Login page", expected: "Form displays" },
      { step: 2, action: "Enter non-existent email", expected: "Email field accepts input" },
      { step: 3, action: "Enter any password", expected: "Password accepted" },
      { step: 4, action: "Click 'Login'", expected: "Authentication checked" },
    ],
    expectedResult:
      "401 Unauthorized – Error: 'Invalid email or password' (generic message for security), no hint about email existence",
    status: "Fail",
  },

  // UC5: Logout
  {
    category: "UC5: Logout",
    id: "LOGOUT-01",
    description: "Logout successfully",
    prerequisites: "User is logged in with valid session token",
    steps: [
      {
        step: 1,
        action: "User is on dashboard with active session",
        expected: "Dashboard displays with 'Logout' button visible",
      },
      { step: 2, action: "Click 'Logout' button in header/menu", expected: "Logout confirmation dialog appears" },
      { step: 3, action: "Confirm logout action", expected: "Logout request sent to server" },
      { step: 4, action: "System invalidates session token", expected: "JWT/session cleared from server" },
    ],
    expectedResult:
      "200 OK – Session terminated, user redirected to login page, cannot access protected routes, success message: 'Logged out successfully'",
    status: "Pass",
  },
  {
    category: "UC5: Logout",
    id: "LOGOUT-02",
    description: "Logout without active session",
    prerequisites: "User session already expired or logged out",
    steps: [
      {
        step: 1,
        action: "Try to access logout endpoint without valid token",
        expected: "Request sent without auth token",
      },
      { step: 2, action: "System checks session validity", expected: "No valid session found" },
    ],
    expectedResult: "401 Unauthorized – Error: 'No active session', user redirected to login page",
    status: "Fail",
  },
  {
    category: "UC5: Logout",
    id: "LOGOUT-03",
    description: "Access protected route after logout",
    prerequisites: "User just logged out",
    steps: [
      { step: 1, action: "Logout successfully", expected: "Session cleared" },
      { step: 2, action: "Try to access dashboard URL directly", expected: "Request sent without valid token" },
      { step: 3, action: "System checks authentication", expected: "No valid session found" },
    ],
    expectedResult: "401 Unauthorized – User redirected to login page with message: 'Please login to continue'",
    status: "Fail",
  },

  // UC6: Update Profile
  {
    category: "UC6: Update Profile",
    id: "PROF-01",
    description: "Update profile with valid data",
    prerequisites: "User logged in, current name: 'John Doe'",
    steps: [
      {
        step: 1,
        action: "Navigate to Profile Settings page",
        expected: "Profile form displays with current user data pre-filled",
      },
      { step: 2, action: "Change name to 'Jane Smith'", expected: "Name field accepts new value" },
      { step: 3, action: "Upload new avatar image (valid format, <5MB)", expected: "Image preview displays" },
      { step: 4, action: "Click 'Save Changes' button", expected: "Update request sent to server" },
      { step: 5, action: "System validates and saves changes", expected: "Database updated" },
    ],
    expectedResult:
      "200 OK – Profile updated successfully, success message displayed, new data reflected across UI, avatar updated",
    status: "Pass",
  },
  {
    category: "UC6: Update Profile",
    id: "PROF-02",
    description: "Update profile with invalid data (empty name)",
    prerequisites: "User logged in",
    steps: [
      { step: 1, action: "Open Profile Settings", expected: "Form displays" },
      { step: 2, action: "Clear name field (leave empty)", expected: "Name field becomes empty" },
      { step: 3, action: "Click 'Save Changes'", expected: "Validation triggered" },
    ],
    expectedResult: "400 Bad Request – Error: 'Name is required', changes not saved, error displayed below name field",
    status: "Fail",
  },
  {
    category: "UC6: Update Profile",
    id: "PROF-03",
    description: "Update profile with oversized avatar",
    prerequisites: "User logged in",
    steps: [
      { step: 1, action: "Navigate to Profile Settings", expected: "Form displays" },
      { step: 2, action: "Try to upload image file >5MB", expected: "File picker opens" },
      { step: 3, action: "Select large image (e.g., 10MB)", expected: "Validation check runs" },
    ],
    expectedResult: "400 Bad Request – Error: 'File size exceeds 5MB limit', upload rejected, previous avatar retained",
    status: "Fail",
  },
  {
    category: "UC6: Update Profile",
    id: "PROF-04",
    description: "Update profile without authentication",
    prerequisites: "User session expired",
    steps: [
      { step: 1, action: "Attempt to access Profile Settings with expired token", expected: "Request sent" },
      { step: 2, action: "System checks authentication", expected: "Token invalid/expired" },
    ],
    expectedResult: "401 Unauthorized – User redirected to login page, changes not saved",
    status: "Fail",
  },

  // UC7: View Profile (GetMe)
  {
    category: "UC7: View Profile (GetMe)",
    id: "GETME-01",
    description: "Retrieve own profile successfully",
    prerequisites: "User logged in with valid token",
    steps: [
      { step: 1, action: "User navigates to dashboard/profile page", expected: "Page loads" },
      {
        step: 2,
        action: "System sends GET request to /api/me endpoint",
        expected: "Request includes valid JWT token in header",
      },
      {
        step: 3,
        action: "System validates token and fetches user data",
        expected: "User record retrieved from database",
      },
    ],
    expectedResult:
      "200 OK – User profile data returned (name, email, avatar, role, created date), displayed correctly on UI",
    status: "Pass",
  },
  {
    category: "UC7: View Profile (GetMe)",
    id: "GETME-02",
    description: "Retrieve profile with expired token",
    prerequisites: "User token expired",
    steps: [
      { step: 1, action: "Send request to /api/me with expired token", expected: "Request sent" },
      { step: 2, action: "System validates token", expected: "Token validation fails (expired)" },
    ],
    expectedResult: "401 Unauthorized – Error: 'Token expired', user redirected to login",
    status: "Fail",
  },
  {
    category: "UC7: View Profile (GetMe)",
    id: "GETME-03",
    description: "Retrieve profile without token",
    prerequisites: "No authentication token provided",
    steps: [
      { step: 1, action: "Send GET request to /api/me without Authorization header", expected: "Request sent" },
      { step: 2, action: "System checks for authentication", expected: "No token found" },
    ],
    expectedResult: "401 Unauthorized – Error: 'Authentication required', access denied",
    status: "Fail",
  },

  // UC8: Get User by ID or Email
  {
    category: "UC8: Get User by ID or Email",
    id: "GETUSER-01",
    description: "Admin retrieves user by ID",
    prerequisites: "Admin logged in, user ID '123' exists",
    steps: [
      { step: 1, action: "Admin navigates to User Management page", expected: "User list displays" },
      { step: 2, action: "Enter user ID '123' in search field", expected: "Search field accepts input" },
      { step: 3, action: "Click 'Search' button", expected: "GET request sent to /api/users/123" },
      { step: 4, action: "System verifies admin role and fetches user", expected: "User data retrieved" },
    ],
    expectedResult: "200 OK – User details displayed (name, email, role, status, projects), admin can view/edit",
    status: "Pass",
  },
  {
    category: "UC8: Get User by ID or Email",
    id: "GETUSER-02",
    description: "Non-admin tries to get user by ID",
    prerequisites: "Regular user logged in (not admin)",
    steps: [
      { step: 1, action: "Regular user tries to access /api/users/123", expected: "Request sent with user token" },
      { step: 2, action: "System checks user role", expected: "Role is 'User', not 'Admin'" },
    ],
    expectedResult: "403 Forbidden – Error: 'Admin access required', request denied",
    status: "Fail",
  },
  {
    category: "UC8: Get User by ID or Email",
    id: "GETUSER-03",
    description: "Admin searches for non-existent user",
    prerequisites: "Admin logged in, user ID '999' does not exist",
    steps: [
      { step: 1, action: "Admin enters user ID '999' in search", expected: "Search initiated" },
      { step: 2, action: "System queries database for user ID 999", expected: "No user found" },
    ],
    expectedResult: "404 Not Found – Error: 'User not found', no data displayed",
    status: "Fail",
  },
  {
    category: "UC8: Get User by ID or Email",
    id: "GETUSER-04",
    description: "Admin retrieves user by email",
    prerequisites: "Admin logged in, user with email 'test@example.com' exists",
    steps: [
      { step: 1, action: "Admin navigates to User Management", expected: "Page loads" },
      { step: 2, action: "Enter email 'test@example.com' in search field", expected: "Email accepted" },
      { step: 3, action: "Click 'Search'", expected: "Request sent to /api/users?email=test@example.com" },
    ],
    expectedResult: "200 OK – User details returned and displayed",
    status: "Pass",
  },

  // UC9: Create Project
  {
    category: "UC9: Create Project",
    id: "PROJ-01",
    description: "Create project with valid data",
    prerequisites: "User authenticated, project name 'Test Project' not exists",
    steps: [
      {
        step: 1,
        action: "Click 'Create Project' button on dashboard",
        expected: "Project creation modal/form displays",
      },
      { step: 2, action: "Enter project name 'Test Project'", expected: "Name field accepts input" },
      {
        step: 3,
        action: "Enter project key 'TEST' (unique)",
        expected: "Key field accepts input, validates uniqueness",
      },
      { step: 4, action: "Enter description (optional)", expected: "Description field accepts input" },
      { step: 5, action: "Click 'Create' button", expected: "Project creation request sent" },
    ],
    expectedResult:
      "201 Created – Project created with user as Owner, default columns (To Do, In Progress, Done) created, initial backlog sprint created, user redirected to project board",
    status: "Pass",
  },
  {
    category: "UC9: Create Project",
    id: "PROJ-02",
    description: "Create project with duplicate name",
    prerequisites: "User already owns project named 'Existing Project'",
    steps: [
      { step: 1, action: "Open Create Project form", expected: "Form displays" },
      { step: 2, action: "Enter name 'Existing Project' (duplicate)", expected: "Name accepted temporarily" },
      { step: 3, action: "Enter unique key 'EXIST'", expected: "Key accepted" },
      { step: 4, action: "Click 'Create'", expected: "Validation check runs" },
    ],
    expectedResult: "400 Bad Request – Error: 'Project name must be unique per owner', project not created",
    status: "Fail",
  },
  {
    category: "UC9: Create Project",
    id: "PROJ-03",
    description: "Create project without authentication",
    prerequisites: "User not logged in or token expired",
    steps: [
      { step: 1, action: "Try to access Create Project page", expected: "Request sent without valid token" },
      { step: 2, action: "System checks authentication", expected: "No valid session" },
    ],
    expectedResult: "401 Unauthorized – User redirected to login page, project not created",
    status: "Fail",
  },
  {
    category: "UC9: Create Project",
    id: "PROJ-04",
    description: "Create project with empty required fields",
    prerequisites: "User authenticated",
    steps: [
      { step: 1, action: "Open Create Project form", expected: "Form displays" },
      { step: 2, action: "Leave project name empty", expected: "Name field empty" },
      { step: 3, action: "Click 'Create'", expected: "Validation triggered" },
    ],
    expectedResult: "400 Validation Error – Error: 'Project name is required', form not submitted",
    status: "Fail",
  },

  // UC10: View Project List
  {
    category: "UC10: View Project List",
    id: "PROJLIST-01",
    description: "View all projects user is member of",
    prerequisites: "User is member of 3 projects",
    steps: [
      { step: 1, action: "User logs in successfully", expected: "Redirected to dashboard" },
      { step: 2, action: "Navigate to Projects page", expected: "Page loads" },
      { step: 3, action: "System fetches projects where user is member", expected: "Database query executes" },
    ],
    expectedResult:
      "200 OK – List of 3 projects displayed with project name, key, owner, member count, last updated date, sorted by last accessed",
    status: "Pass",
  },
  {
    category: "UC10: View Project List",
    id: "PROJLIST-02",
    description: "View project list with pagination",
    prerequisites: "User is member of 25 projects",
    steps: [
      { step: 1, action: "Navigate to Projects page", expected: "Page loads" },
      { step: 2, action: "System applies pagination (20 per page)", expected: "First 20 projects displayed" },
      { step: 3, action: "Click 'Next' or page 2", expected: "Page 2 loads" },
    ],
    expectedResult:
      "200 OK – Page 1 shows 20 projects, Page 2 shows remaining 5 projects, pagination controls functional",
    status: "Pass",
  },
  {
    category: "UC10: View Project List",
    id: "PROJLIST-03",
    description: "View empty project list",
    prerequisites: "User not member of any project",
    steps: [
      { step: 1, action: "Navigate to Projects page", expected: "Page loads" },
      { step: 2, action: "System queries user's projects", expected: "No projects found" },
    ],
    expectedResult: "200 OK – Empty state displayed with message: 'No projects yet' and 'Create Project' button",
    status: "Pass",
  },
  {
    category: "UC10: View Project List",
    id: "PROJLIST-04",
    description: "Filter starred projects",
    prerequisites: "User has 5 projects, 2 are starred",
    steps: [
      { step: 1, action: "Navigate to Projects page", expected: "All 5 projects displayed" },
      { step: 2, action: "Click 'Starred' filter option", expected: "Filter applied" },
      { step: 3, action: "System filters for starred projects only", expected: "Query filtered" },
    ],
    expectedResult: "200 OK – Only 2 starred projects displayed at top of list with star icon highlighted",
    status: "Pass",
  },

  // UC11: Update Project Info
  {
    category: "UC11: Update Project Info",
    id: "PROJUPD-01",
    description: "Update project name as Owner",
    prerequisites: "User is Project Owner, project 'Old Name' exists",
    steps: [
      { step: 1, action: "Navigate to project settings", expected: "Settings page displays" },
      { step: 2, action: "Change project name to 'New Name'", expected: "Name field updated" },
      { step: 3, action: "Click 'Save Changes'", expected: "Update request sent" },
      { step: 4, action: "System validates ownership and updates", expected: "Database updated" },
    ],
    expectedResult:
      "200 OK – Project name updated to 'New Name', activity log entry created, all members notified, cache invalidated",
    status: "Pass",
  },
  {
    category: "UC11: Update Project Info",
    id: "PROJUPD-02",
    description: "Member tries to update project settings",
    prerequisites: "User is regular Member (not Admin/Owner)",
    steps: [
      { step: 1, action: "Try to access project settings", expected: "Request sent" },
      { step: 2, action: "System checks user role", expected: "Role is 'Member', not authorized" },
    ],
    expectedResult: "403 Forbidden – Error: 'Only Admin or Owner can modify project settings', changes rejected",
    status: "Fail",
  },
  {
    category: "UC11: Update Project Info",
    id: "PROJUPD-03",
    description: "Try to change project key",
    prerequisites: "User is Owner, project key is 'TEST'",
    steps: [
      { step: 1, action: "Navigate to project settings", expected: "Settings page displays" },
      { step: 2, action: "Try to edit project key field", expected: "Field is disabled/read-only" },
      { step: 3, action: "Attempt to modify key via API call", expected: "Request sent with new key" },
    ],
    expectedResult: "400 Bad Request – Error: 'Project key cannot be changed after creation', key remains unchanged",
    status: "Fail",
  },
  {
    category: "UC11: Update Project Info",
    id: "PROJUPD-04",
    description: "Update project description successfully",
    prerequisites: "User is Admin of project",
    steps: [
      { step: 1, action: "Open project settings", expected: "Settings form displays" },
      { step: 2, action: "Update description field with new text", expected: "Description field accepts input" },
      { step: 3, action: "Click 'Save'", expected: "Update request sent" },
      { step: 4, action: "System validates role and saves", expected: "Changes saved to database" },
    ],
    expectedResult: "200 OK – Description updated, activity log created, members notified of project changes",
    status: "Pass",
  },
  {
    category: "UC11: Update Project Info",
    id: "PROJUPD-05",
    description: "Update archived project",
    prerequisites: "Project is archived",
    steps: [
      { step: 1, action: "Try to access settings of archived project", expected: "Request sent" },
      { step: 2, action: "System checks project status", expected: "Status is 'Archived'" },
      { step: 3, action: "Attempt to modify settings", expected: "Validation runs" },
    ],
    expectedResult: "403 Forbidden – Error: 'Cannot modify archived project', changes rejected",
    status: "Fail",
  },

  // UC12: Delete Project
  {
    category: "UC12: Delete Project",
    id: "PROJDEL-01",
    description: "Delete project as Owner",
    prerequisites: "User is Project Owner, project 'Test Project' exists",
    steps: [
      { step: 1, action: "Navigate to project settings", expected: "Settings page displays" },
      { step: 2, action: "Click 'Delete Project' button", expected: "Confirmation dialog appears" },
      { step: 3, action: "Type project name 'Test Project' to confirm", expected: "Confirmation text matches" },
      { step: 4, action: "Click 'Confirm Delete'", expected: "Delete request sent" },
      { step: 5, action: "System soft-deletes project", expected: "Project marked as deleted" },
    ],
    expectedResult:
      "200 OK – Project soft-deleted, all members lose access, removed from member lists, scheduled for hard delete in 30 days, user redirected to projects list",
    status: "Pass",
  },
  {
    category: "UC12: Delete Project",
    id: "PROJDEL-02",
    description: "Non-owner tries to delete project",
    prerequisites: "User is Admin (not Owner)",
    steps: [
      { step: 1, action: "Navigate to project settings", expected: "Settings visible" },
      { step: 2, action: "Look for Delete Project option", expected: "Delete button not visible or disabled" },
      { step: 3, action: "Try to call delete API directly", expected: "Request sent" },
      { step: 4, action: "System checks ownership", expected: "User is not Owner" },
    ],
    expectedResult: "403 Forbidden – Error: 'Only Project Owner can delete project', project not deleted",
    status: "Fail",
  },
  {
    category: "UC12: Delete Project",
    id: "PROJDEL-03",
    description: "Delete project without confirmation",
    prerequisites: "User is Owner",
    steps: [
      { step: 1, action: "Click 'Delete Project'", expected: "Confirmation dialog shows" },
      { step: 2, action: "Type incorrect project name 'Wrong Name'", expected: "Name doesn't match" },
      { step: 3, action: "Click 'Confirm Delete'", expected: "Validation runs" },
    ],
    expectedResult: "400 Bad Request – Error: 'Project name does not match', deletion cancelled",
    status: "Fail",
  },
  {
    category: "UC12: Delete Project",
    id: "PROJDEL-04",
    description: "View deleted project after 30 days",
    prerequisites: "Project soft-deleted 31 days ago",
    steps: [
      { step: 1, action: "System scheduled cleanup runs", expected: "Cleanup job executes" },
      { step: 2, action: "Try to access deleted project URL", expected: "Request sent" },
      { step: 3, action: "System checks project status", expected: "Project hard-deleted (not found)" },
    ],
    expectedResult: "404 Not Found – Error: 'Project not found', all associated data permanently deleted",
    status: "Fail",
  },
];

// 🧾 Định dạng mỗi test case thành 1 hàng
function formatTestCase(tc) {
  const stepsFormatted = tc.steps.map((s) => `Step ${s.step}: ${s.action}\nExpected: ${s.expected}`).join("\n\n");

  return {
    "": tc.category,
    "Test Case ID": tc.id,
    "Test Description": tc.description,
    PreRequisites: tc.prerequisites,
    Steps: stepsFormatted,
    "Step Expected Result": "",
    "Expected Result": tc.expectedResult,
    "Actual Result": "",
    Status: tc.status === "Pass" ? "Passed" : "Failed",
    Note: "",
  };
}

// 📖 Đọc file Excel
console.log("Đang đọc file Excel...");
const workbook = XLSX.readFile(INPUT_FILE);

const sheetName = "Function Test Cases";
const worksheet = workbook.Sheets[sheetName];
if (!worksheet) {
  throw new Error(`❌ Không tìm thấy sheet: ${sheetName}`);
}

// 🔍 Tìm dòng đầu tiên trống (bắt đầu từ dòng 17 theo template)
let insertRow = 17;
const range = XLSX.utils.decode_range(worksheet["!ref"] || "A1");
while (insertRow <= range.e.r + 1) {
  const cell = worksheet[XLSX.utils.encode_cell({ r: insertRow - 1, c: 1 })]; // cột B
  if (!cell || cell.v === undefined || cell.v === null || cell.v === "") {
    break;
  }
  insertRow++;
}

console.log(`🔍 Sẽ chèn từ dòng ${insertRow}`);

// 🧮 Chuyển đổi dữ liệu
const newRows = testCases.map(formatTestCase);

// ✍️ Ghi vào worksheet
newRows.forEach((row, idx) => {
  const rowIndex = insertRow + idx;
  const headers = [
    "",
    "Test Case ID",
    "Test Description",
    "PreRequisites",
    "Steps",
    "Step Expected Result",
    "Expected Result",
    "Actual Result",
    "Status",
    "Note",
  ];

  headers.forEach((colName, colIndex) => {
    const cellAddress = XLSX.utils.encode_cell({ r: rowIndex - 1, c: colIndex });
    worksheet[cellAddress] = { t: "s", v: row[colName] || "" };
  });

  if (rowIndex - 1 > range.e.r) range.e.r = rowIndex - 1;
});
worksheet["!ref"] = XLSX.utils.encode_range(range);

// 💾 Ghi file mới
const outputBuffer = XLSX.write(workbook, { bookType: "xlsx", type: "buffer" });
await writeFile(OUTPUT_FILE, outputBuffer);

console.log(`✅ Xuất thành công file: ${OUTPUT_FILE}`);
