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
    REJECTED,
    DIPLOMA_VERIFIED
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

public enum InvoiceStatus {
    PENDING,
    PAID,
    CANCELLED,
    REFUNDED
}

public enum PaymentStatus {
    PENDING,
    PROCESSING,
    SUCCEEDED,
    FAILED,
    CANCELLED,
    REFUNDED
}

public enum PaymentMethod {
    CARD,
    BANK_TRANSFER,
    PAYPAL,
    OTHER
}

public enum DocumentType {
    ID_PAPER,
    DIPLOMA
}

public enum DocumentStatus {
    PENDING,
    APPROVED,
    REJECTED
}
