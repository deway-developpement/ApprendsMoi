namespace backend.Database.Models;

public enum ProfileType : int {
    Admin,
    Teacher,
    Parent,
    Student
}

public enum GradeLevel {
    CP,
    CE1,
    CE2,
    CM1,
    CM2,
    SIXIEME,
    CINQUIEME,
    QUATRIEME,
    TROISIEME,
    SECONDE,
    PREMIERE,
    TERMINALE
}

public enum VerificationStatus {
    PENDING,
    VERIFIED,
    REJECTED
}

public enum CourseStatus {
    PENDING,
    CONFIRMED,
    COMPLETED,
    CANCELLED,
    DISPUTED
}

public enum CourseFormat {
    HOME,
    VISIO
}
