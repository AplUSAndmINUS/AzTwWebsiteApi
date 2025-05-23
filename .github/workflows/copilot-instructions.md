# Copilot Instructions

This file contains guidelines and instructions for using GitHub Copilot to assist in building and debugging the `mcp_server` project.

## General Guidelines
- Use Python 3.13 for all development.
- Follow PEP 8 coding standards.
<!-- - Ensure all dependencies are listed in `requirements.txt`. -->
- Use `.gitignore` to exclude unnecessary files and folders from commits.
- Integrate Jira and Bitbucket Cloud for issue tracking and version control.

## Runtime Environment
- Use npm commands for managing JavaScript dependencies and frontend components.
- Use Python 3.13 for backend development.

## Development Workflow
1. Create a feature branch for each new task or bug fix.
2. Write clean, modular, and well-documented code.
3. Test all changes locally before committing.
<!-- 4. Implement Fluent UI from the `@fluentui/react` library for the frontend. -->
<!-- 5. Implement the "useSPFxTheme" hook on any front-end components that require it. -->
6. Prioritize JSX Fluent UI over SCSS modules wherever possible.
7. Utilize the module.scss file within any component anywhere JSX is not possible to use.
8. Ensure storybook is used for all components.
9. Where appropriate, use GitHub Copilot to assist with code generation and debugging.
10. With a bug fix, ensure the bug is reproducible before starting work.
11. For new features, write unit tests to cover the new functionality.
12. Use descriptive commit messages that follow the format:
   ```
   [JIRA-123] Short description of the change
   ```
13. Push changes to the remote repository and create a pull request for review.
14. Address any feedback from code reviews promptly.
15. Ensure all tests pass before merging.
16. Merge the pull request into the main branch once approved.
17. Delete the feature branch after merging to keep the repository clean.
18. Document any major changes in the project README.

## Security Best Practices
- Store sensitive information in environment variables (e.g., `.env` file).
- Regularly update dependencies to address security vulnerabilities.
- Use HTTPS for all communications.

## Additional Notes
- Ensure the server is secure and follows best practices for Python development.
- Document all major changes in the project README.

---

Feel free to update this file as the project evolves.
