# Contributing to AutoDocOps

## Security Guidelines

### Secret Management Policy

This project implements a comprehensive secret detection and management policy to prevent sensitive information exposure.

#### Secret Detection
- **Automated Scanning**: Gitleaks runs on every push and PR via GitHub Actions
- **Local Scanning**: Run `scripts/fix_critical.sh` before committing
- **Daily Audits**: Scheduled security scans at 3 AM UTC

#### Supported Secret Types
The security scanning detects:
- API Keys (generic patterns and service-specific)
- Database credentials and connection strings
- JWT tokens and authentication secrets
- Cloud provider credentials (AWS, Azure, GCP)
- Stripe webhook secrets and API keys
- Private keys and certificates
- Authentication tokens (Bearer, OAuth)

#### Emergency Response
If secrets are detected:
1. **STOP** - Do not push the code
2. **Remove** the secret from code and history
3. **Rotate** the compromised credential immediately
4. **Notify** the security team
5. **Document** the incident

### Data Privacy Compliance

#### Organization ID Masking
- All organization IDs are hashed using SHA256 before logging
- 12-character hash prefix provides collision resistance for high-traffic scenarios
- GDPR compliant - no personally identifiable information in logs

```csharp
// Example: Organization ID masking
var hashedId = AnonymizeOrganizationId(organizationId);
logger.LogInformation("Processing request for org: {OrganizationId}", hashedId);
```

#### Webhook Security
- Stripe webhook signatures are validated using secure comparison
- Invalid signatures return proper HTTP 400 with ProblemDetails
- No sensitive information exposed in error responses

### Code Quality Standards

#### Overflow Protection
All arithmetic operations that could overflow use checked contexts:

```csharp
// Exponential backoff with overflow protection
try
{
    return TimeSpan.FromTicks(checked(current.Ticks * 2));
}
catch (OverflowException)
{
    return TimeSpan.MaxValue;
}
```

#### Testing Architecture
- Use `InternalsVisibleTo` instead of reflection for internal access
- Comprehensive test coverage including edge cases and overflow scenarios
- Clean architecture patterns with proper dependency injection

### Development Workflow

#### Pre-commit Checklist
1. [ ] Run `scripts/fix_critical.sh` for secret detection
2. [ ] Verify all tests pass: `dotnet test`
3. [ ] Check for vulnerable dependencies: `dotnet list package --vulnerable`
4. [ ] Validate that no TODOs reference Azure endpoints without proper validation

#### Security Testing
- **Unit Tests**: Cover overflow scenarios and edge cases
- **Integration Tests**: Validate webhook signature verification
- **Security Tests**: Ensure proper data masking and secret handling

### CI/CD Security

#### GitHub Actions
- **Secret Scanning**: Automated with Gitleaks on every push/PR
- **Dependency Audit**: Check for vulnerable NuGet packages
- **SARIF Upload**: Security findings integrated with GitHub Security tab

#### Deployment Security
- Secrets managed via Azure Key Vault in production
- Environment-specific configuration isolation
- Automated security patching for container images

### Incident Response

#### Secret Exposure Response
1. **Immediate Actions**:
   - Revoke compromised credentials
   - Remove from Git history: `git filter-branch` or BFG
   - Force push cleaned history (coordinate with team)

2. **Assessment**:
   - Determine scope of exposure
   - Check logs for potential unauthorized access
   - Document timeline and impact

3. **Recovery**:
   - Generate new credentials
   - Update all affected systems
   - Monitor for suspicious activity

#### Reporting
- Security incidents: `security@company.com`
- General issues: GitHub Issues with `security` label
- Urgent matters: Escalate through on-call rotation

### Tools and Resources

#### Required Tools
- **Gitleaks**: For local secret scanning
- **dotnet-audit**: For dependency vulnerability scanning
- **SonarQube**: For code quality analysis (optional)

#### Installation
```powershell
# Install gitleaks (Windows)
winget install gitleaks

# Or via GitHub releases
Invoke-WebRequest -Uri "https://github.com/gitleaks/gitleaks/releases/latest/download/gitleaks_windows_x64.zip" -OutFile "gitleaks.zip"
```

#### Configuration
Security tools configuration:
- **Gitleaks**: `.gitleaks.toml` (if custom rules needed)
- **Scripts**: `scripts/fix_critical.sh` for comprehensive scanning

### Code Review Security Checklist

#### For Reviewers
- [ ] No hardcoded secrets or credentials
- [ ] Proper error handling without information disclosure
- [ ] Overflow protection for arithmetic operations
- [ ] Secure comparison for sensitive data
- [ ] Appropriate logging levels (no sensitive data in logs)
- [ ] Input validation for all external inputs
- [ ] Proper authentication and authorization checks

#### For Authors
- [ ] Ran security scan locally before submitting PR
- [ ] Added appropriate tests for security-critical code
- [ ] Documented any security assumptions or requirements
- [ ] Followed principle of least privilege
- [ ] Used framework security features (ASP.NET Core security headers, etc.)

### Security Contacts

- **Security Team**: `security@company.com`
- **DevOps Team**: `devops@company.com`
- **Compliance Officer**: `compliance@company.com`

---

> 🔒 **Remember**: Security is everyone's responsibility. When in doubt, ask the security team.
